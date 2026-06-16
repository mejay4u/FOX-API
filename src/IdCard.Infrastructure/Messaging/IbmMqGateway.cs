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
/// IBM WebSphere MQ gateway.
/// Opens separate managed connections for the PUT (request) and GET (reply) queues
/// because they can live on different hosts / queue managers.
/// All IBM MQ calls are synchronous; offloaded to the thread-pool via Task.Run.
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

    public Task<MqIdCardResponse> RequestIdCardAsync(MqIdCardRequest request, CancellationToken ct = default)
        => Task.Run(() => DoRequestReply(request), ct);

    // ── core synchronous logic ───────────────────────────────────────────────

    private MqIdCardResponse DoRequestReply(MqIdCardRequest request)
    {
        try
        {
            EnsureConnected();
        }
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

        // Guard: EnsureConnected must have set both managers
        if (_putQm is null || _getQm is null)
            return Fail("CONNECT_FAILED", "Queue manager connection could not be established.");

        MQQueue? putQueue = null;
        MQQueue? getQueue = null;

        try
        {
            // ── Build XML from template ──────────────────────────────────────
            var xml = IdCardRequestXmlBuilder.Build(
                request.MemberId,
                request.SubscriberId,
                request.Lob,
                _opts.Environment,
                _contentRootPath);

            // ── PUT to INBOUND request queue ─────────────────────────────────
            putQueue = _putQm!.AccessQueue(
                _opts.PutQueue,
                MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING);

            var putMsg = new MQMessage
            {
                Format           = MQC.MQFMT_STRING,
                CharacterSet     = 1208,             // UTF-8
                ReplyToQueueName = _opts.GetQueue
            };
            putMsg.WriteString(xml);

            putQueue.Put(putMsg, new MQPutMessageOptions());

            var msgId = BitConverter.ToString(putMsg.MessageId);
            _logger.LogInformation(
                "IBM MQ PUT — ID card requested. MemberId={MemberId} Queue={Queue} MsgId={MsgId}",
                request.MemberId, _opts.PutQueue, msgId);

            // ── GET acknowledgement from OUTBOUND reply queue ─────────────────
            getQueue = _getQm!.AccessQueue(
                _opts.GetQueue,
                MQC.MQOO_INPUT_EXCLUSIVE | MQC.MQOO_FAIL_IF_QUIESCING);

            var replyMsg = new MQMessage
            {
                CorrelationId = putMsg.MessageId,
                CharacterSet  = 1208   // request conversion to UTF-8 on MQGMO_CONVERT
            };

            var gmo = new MQGetMessageOptions
            {
                WaitInterval = _opts.TimeoutMs,
                MatchOptions = MQC.MQMO_MATCH_CORREL_ID,
                Options      = MQC.MQGMO_WAIT | MQC.MQGMO_CONVERT
            };

            getQueue.Get(replyMsg, gmo);

            _logger.LogInformation(
                "IBM MQ GET — acknowledgement received. MemberId={MemberId} Queue={Queue}",
                request.MemberId, _opts.GetQueue);

            return new MqIdCardResponse { IsSuccess = true, MessageId = msgId };
        }
        catch (MQException mqEx)
        {
            _logger.LogError(
                mqEx,
                "IBM MQ error. MemberId={MemberId} ReasonCode={RC}",
                request.MemberId, mqEx.ReasonCode);

            if (mqEx.ReasonCode is MQC.MQRC_CONNECTION_BROKEN or MQC.MQRC_NOT_CONNECTED)
                ResetConnections();

            return Fail($"MQ_{mqEx.ReasonCode}", mqEx.Message);
        }
        finally
        {
            try { putQueue?.Close(); } catch { /* best-effort */ }
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
                _putQm = Connect(
                    _opts.PutQueueManager,
                    _opts.PutHost,
                    _opts.PutPort,
                    _opts.PutChannel);

                _logger.LogInformation(
                    "Connected to PUT QueueManager={QM} {Host}:{Port} Channel={Ch}",
                    _opts.PutQueueManager, _opts.PutHost, _opts.PutPort, _opts.PutChannel);
            }

            if (_getQm?.IsConnected != true)
            {
                _getQm = Connect(
                    _opts.GetQueueManager,
                    _opts.GetHost,
                    _opts.GetPort,
                    _opts.GetChannel);

                _logger.LogInformation(
                    "Connected to GET QueueManager={QM} {Host}:{Port} Channel={Ch}",
                    _opts.GetQueueManager, _opts.GetHost, _opts.GetPort, _opts.GetChannel);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static MQQueueManager Connect(
        string queueManager, string host, string port, string channel)
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
