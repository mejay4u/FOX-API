using System.Collections;
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
/// PUTs an ID card request message then GETs the acknowledgement reply.
/// All IBM MQ calls are synchronous; offloaded to the thread-pool via Task.Run.
/// </summary>
public sealed class IbmMqGateway : IIdCardMqGateway, IDisposable
{
    private readonly MqOptions _opts;
    private readonly string _contentRootPath;
    private readonly ILogger<IbmMqGateway> _logger;
    private MQQueueManager? _qm;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public IbmMqGateway(IOptions<MqOptions> options, IHostEnvironment env, ILogger<IbmMqGateway> logger)
    {
        _opts            = options.Value;
        _contentRootPath = env.ContentRootPath;
        _logger          = logger;
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
            return Fail($"MQ_{ex.ReasonCode}", ex.Message);
        }

        MQQueue? requestQ = null;
        MQQueue? replyQ   = null;

        try
        {
            // ── Build XML from template ──────────────────────────────────────
            var xml = IdCardRequestXmlBuilder.Build(
                request.MemberId,
                request.SubscriberId,
                request.Lob,
                _opts.Environment,
                _contentRootPath);

            // ── PUT to request queue ─────────────────────────────────────────
            requestQ = _qm!.AccessQueue(
                _opts.RequestQueueName,
                MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING);

            var putMsg = new MQMessage
            {
                Format           = MQC.MQFMT_STRING,
                CharacterSet     = 1208,                 // UTF-8
                ReplyToQueueName = _opts.ReplyQueueName
            };
            putMsg.WriteString(xml);

            requestQ.Put(putMsg, new MQPutMessageOptions());

            var msgId = BitConverter.ToString(putMsg.MessageId);
            _logger.LogInformation(
                "IBM MQ PUT — ID card requested. MemberId={MemberId} MsgId={MsgId}",
                request.MemberId, msgId);

            // ── GET acknowledgement from reply queue ─────────────────────────
            replyQ = _qm.AccessQueue(
                _opts.ReplyQueueName,
                MQC.MQOO_INPUT_EXCLUSIVE | MQC.MQOO_FAIL_IF_QUIESCING);

            var replyMsg = new MQMessage
            {
                CorrelationId = putMsg.MessageId
            };

            var gmo = new MQGetMessageOptions
            {
                WaitInterval = _opts.TimeoutMs,
                MatchOptions = MQC.MQMO_MATCH_CORREL_ID,
                Options      = MQC.MQGMO_WAIT | MQC.MQGMO_CONVERT
            };

            replyQ.Get(replyMsg, gmo);

            _logger.LogInformation(
                "IBM MQ GET — acknowledgement received. MemberId={MemberId}", request.MemberId);

            return new MqIdCardResponse { IsSuccess = true, MessageId = msgId };
        }
        catch (MQException mqEx)
        {
            _logger.LogError(
                mqEx,
                "IBM MQ error. MemberId={MemberId} ReasonCode={RC}",
                request.MemberId, mqEx.ReasonCode);

            if (mqEx.ReasonCode is MQC.MQRC_CONNECTION_BROKEN or MQC.MQRC_NOT_CONNECTED)
                ResetConnection();

            return Fail($"MQ_{mqEx.ReasonCode}", mqEx.Message);
        }
        finally
        {
            try { requestQ?.Close(); } catch { /* best-effort */ }
            try { replyQ?.Close();   } catch { /* best-effort */ }
        }
    }

    // ── connection management ────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (_qm?.IsConnected == true)
            return;

        _lock.Wait();
        try
        {
            if (_qm?.IsConnected == true)
                return;

            var props = new Hashtable
            {
                [MQC.HOST_NAME_PROPERTY] = _opts.ConnectionName,
                [MQC.PORT_PROPERTY]      = _opts.Port,
                [MQC.CHANNEL_PROPERTY]   = _opts.Channel,
                [MQC.TRANSPORT_PROPERTY] = MQC.TRANSPORT_MQSERIES_MANAGED
            };

            _qm = new MQQueueManager(_opts.QueueManagerName, props);

            _logger.LogInformation(
                "Connected to IBM MQ. QueueManager={QM} Host={Host}:{Port}",
                _opts.QueueManagerName, _opts.ConnectionName, _opts.Port);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void ResetConnection()
    {
        try { _qm?.Disconnect(); } catch { /* ignore */ }
        _qm = null;
    }

    private static MqIdCardResponse Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResetConnection();
        _lock.Dispose();
    }
}
