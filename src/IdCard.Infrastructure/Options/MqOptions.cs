namespace IdCard.Infrastructure.Options;

public sealed class MqOptions
{
    public const string SectionName = "IbmMq";

    public bool Enabled { get; set; } = false;

    // PUT (request) queue connection
    public string PutHost { get; set; } = string.Empty;
    public string PutPort { get; set; } = "1414";
    public string PutChannel { get; set; } = string.Empty;
    public string PutQueueManager { get; set; } = string.Empty;
    public string PutQueue { get; set; } = string.Empty;

    // GET (reply) queue connection
    public string GetHost { get; set; } = string.Empty;
    public string GetPort { get; set; } = "1414";
    public string GetChannel { get; set; } = string.Empty;
    public string GetQueueManager { get; set; } = string.Empty;
    public string GetQueue { get; set; } = string.Empty;

    public string Environment { get; set; } = "PROD";
    public int TimeoutMs { get; set; } = 30_000;
}
