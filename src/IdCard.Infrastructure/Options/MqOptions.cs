namespace IdCard.Infrastructure.Options;

public sealed class MqOptions
{
    public const string SectionName = "IbmMq";

    public bool Enabled { get; set; } = false;
    public string QueueManagerName { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public int Port { get; set; } = 1414;
    public string RequestQueueName { get; set; } = string.Empty;
    public string ReplyQueueName { get; set; } = string.Empty;
    public string Environment { get; set; } = "PROD";
    public int TimeoutMs { get; set; } = 30_000;
}
