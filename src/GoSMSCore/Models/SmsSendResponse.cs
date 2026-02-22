using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class SmsSendResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("messageId")]
    public int MessageId { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("sendAt")]
    public string? SendAt { get; set; }

    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    [JsonPropertyName("encode")]
    public string? Encode { get; set; }

    [JsonPropertyName("segment")]
    public int Segment { get; set; }

    [JsonPropertyName("smsCharacters")]
    public int SmsCharacters { get; set; }
}
