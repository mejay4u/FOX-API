using System.Collections;
using System.Text;
using IBM.WMQ;
using IdCard.Application.Interfaces;
using IdCard.Domain.Models;
using IdCard.Infrastructure.Messaging.Xml;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Messaging;

/// <summary>
/// IBM WebSphere MQ gateway with separate PUT and polling-GET operations,
/// mirroring the MemberCardAggregator / GetIDCardTransaction pattern.
/// </summary>
public sealed class IbmMqGateway : IIdCardMqGateway, IDisposable
{
    private readonly MqOptions _opts;
    private readonly string _contentRootPath;
    private readonly ILogger<IbmMqGateway> _logger;

    private MQQueueManager? _putQm;
    private MQQueueManager? _getQm;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public IbmMqGateway(IOptions<MqOptions> options, IHostEnvironment env, ILogger<IbmMqGateway> logger)
    {
        _opts            = options.Value;
        _contentRootPath = env.ContentRootPath;
        _logger          = logger;

        // Register legacy Windows codepages (e.g. 1252) required by IBM MQ managed client
        // for character-set conversion of incoming messages.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // ── Step 1: PUT ──────────────────────────────────────────────────────────

    public Task<MqIdCardResponse> PutIdCardRequestAsync(MqIdCardRequest request, CancellationToken ct = default)
        => Task.Run(() => DoPut(request), ct);

    private MqIdCardResponse DoPut(MqIdCardRequest request)
    {
        try { EnsureConnected(); }
        catch (MQException ex)
        {
            _logger.LogError(ex, "IBM MQ connect failed. ReasonCode={RC}", ex.ReasonCode);
            return Fail($"MQ_{ex.ReasonCode}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IBM MQ connect failed with unexpected error");
            return Fail("CONNECT_FAILED", ex.Message);
        }

        if (_putQm is null)
            return Fail("CONNECT_FAILED", "PUT queue manager is not connected.");

        MQQueue? putQueue = null;
        try
        {
            var xml = IdCardRequestXmlBuilder.Build(
                request.MemberId,
                request.PlanId,
                _opts.Environment,
                _contentRootPath);

            putQueue = _putQm.AccessQueue(
                _opts.PutQueue,
                MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING);

            var putMsg = new MQMessage
            {
                Format           = MQC.MQFMT_STRING,
                CharacterSet     = 1208,
                ReplyToQueueName = _opts.GetQueue
            };
            putMsg.WriteString(xml);

            putQueue.Put(putMsg, new MQPutMessageOptions());

            var msgId = BitConverter.ToString(putMsg.MessageId);
            _logger.LogInformation(
                "IBM MQ PUT success. MemberId={MemberId} Queue={Queue} MsgId={MsgId}",
                request.MemberId, _opts.PutQueue, msgId);

            return new MqIdCardResponse { IsSuccess = true, MessageId = msgId };
        }
        catch (MQException mqEx)
        {
            _logger.LogError(mqEx, "IBM MQ PUT error. MemberId={MemberId} RC={RC}",
                request.MemberId, mqEx.ReasonCode);

            if (mqEx.ReasonCode is MQC.MQRC_CONNECTION_BROKEN or MQC.MQRC_NOT_CONNECTED)
                ResetConnections();

            return Fail($"MQ_{mqEx.ReasonCode}", mqEx.Message);
        }
        finally
        {
            try { putQueue?.Close(); } catch { /* best-effort */ }
        }
    }

    // ── Step 2: GetIDCardTransaction — polling GET ───────────────────────────

    public Task<MqIdCardResponse> GetIdCardTransactionAsync(
        string correlationId, string memberId, CancellationToken ct = default)
        => Task.Run(() => DoPollingGet(correlationId, memberId, ct), ct);

    private MqIdCardResponse DoPollingGet(string correlationId, string memberId, CancellationToken ct)
    {
        if (_getQm is null)
            return Fail("CONNECT_FAILED", "GET queue manager is not connected.");

        // Convert hex string back to byte[] for MQ correlation matching
        var correlBytes = correlationId
            .Split('-')
            .Select(b => Convert.ToByte(b, 16))
            .ToArray();

        MQQueue? getQueue = null;
        try
        {
            getQueue = _getQm.AccessQueue(
                _opts.GetQueue,
                MQC.MQOO_INPUT_EXCLUSIVE | MQC.MQOO_FAIL_IF_QUIESCING);

            // Polling loop — mirrors GetIDCardTransaction retry pattern
            for (int attempt = 1; attempt <= _opts.MaxPollAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                Thread.Sleep(_opts.PollIntervalMs);  // wait 1 second between polls

                try
                {
                    var replyMsg = new MQMessage
                    {
                        CorrelationId = correlBytes,
                        CharacterSet  = 1208
                    };

                    var gmo = new MQGetMessageOptions
                    {
                        MatchOptions = MQC.MQMO_MATCH_CORREL_ID,
                        Options      = MQC.MQGMO_NO_WAIT | MQC.MQGMO_CONVERT  // non-blocking poll
                    };

                    getQueue.Get(replyMsg, gmo);

                    // Message received
                    _logger.LogInformation(
                        "IBM MQ GET success on attempt {Attempt}/{Max}. MemberId={MemberId}",
                        attempt, _opts.MaxPollAttempts, memberId);

                    return new MqIdCardResponse { IsSuccess = true, MessageId = correlationId };
                }
                catch (MQException mqEx) when (mqEx.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    // No message yet — continue polling
                    _logger.LogDebug(
                        "IBM MQ no reply yet. Attempt {Attempt}/{Max}. MemberId={MemberId}",
                        attempt, _opts.MaxPollAttempts, memberId);
                }
            }

            // All attempts exhausted
            _logger.LogWarning(
                "IBM MQ GET timed out after {Max} attempts. MemberId={MemberId}",
                _opts.MaxPollAttempts, memberId);

            return Fail("MQ_TIMEOUT",
                $"No reply received after {_opts.MaxPollAttempts} attempts for MemberId={memberId}");
        }
        catch (MQException mqEx)
        {
            _logger.LogError(mqEx, "IBM MQ GET error. MemberId={MemberId} RC={RC}",
                memberId, mqEx.ReasonCode);

            if (mqEx.ReasonCode is MQC.MQRC_CONNECTION_BROKEN or MQC.MQRC_NOT_CONNECTED)
                ResetConnections();

            return Fail($"MQ_{mqEx.ReasonCode}", mqEx.Message);
        }
        finally
        {
            try { getQueue?.Close(); } catch { /* best-effort */ }
        }
    }

    // ── connection management ────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (_putQm?.IsConnected == true && _getQm?.IsConnected == true)
            return;

        _lock.Wait();
        try
        {
            if (_putQm?.IsConnected != true)
            {
                _putQm = Connect(_opts.PutQueueManager, _opts.PutHost, _opts.PutPort, _opts.PutChannel);
                _logger.LogInformation(
                    "Connected to PUT QM={QM} {Host}:{Port} Channel={Ch}",
                    _opts.PutQueueManager, _opts.PutHost, _opts.PutPort, _opts.PutChannel);
            }

            if (_getQm?.IsConnected != true)
            {
                _getQm = Connect(_opts.GetQueueManager, _opts.GetHost, _opts.GetPort, _opts.GetChannel);
                _logger.LogInformation(
                    "Connected to GET QM={QM} {Host}:{Port} Channel={Ch}",
                    _opts.GetQueueManager, _opts.GetHost, _opts.GetPort, _opts.GetChannel);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static MQQueueManager Connect(string queueManager, string host, string port, string channel)
    {
        var props = new Hashtable
        {
            [MQC.HOST_NAME_PROPERTY] = host,
            [MQC.PORT_PROPERTY]      = int.TryParse(port, out var p) ? p : 1414,
            [MQC.CHANNEL_PROPERTY]   = channel,
            [MQC.TRANSPORT_PROPERTY] = MQC.TRANSPORT_MQSERIES_MANAGED
        };
        return new MQQueueManager(queueManager, props);
    }

    private void ResetConnections()
    {
        try { _putQm?.Disconnect(); } catch { /* ignore */ }
        try { _getQm?.Disconnect(); } catch { /* ignore */ }
        _putQm = null;
        _getQm = null;
    }

    private static MqIdCardResponse Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResetConnections();
        _lock.Dispose();
    }
}
