using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class BulkSmsResult
{
    [JsonPropertyName("messageId")]
    public int MessageId { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
