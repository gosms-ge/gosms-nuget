using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class SendBulkSmsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }

    [JsonPropertyName("failedCount")]
    public int FailedCount { get; set; }

    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("encode")]
    public string? Encode { get; set; }

    [JsonPropertyName("segment")]
    public int Segment { get; set; }

    [JsonPropertyName("smsCharacters")]
    public int SmsCharacters { get; set; }

    [JsonPropertyName("messages")]
    public List<BulkSmsResult>? Messages { get; set; }
}
