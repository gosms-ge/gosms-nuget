using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class OtpSendResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("balance")]
    public int Balance { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("sendAt")]
    public string? SendAt { get; set; }

    [JsonPropertyName("encode")]
    public string? Encode { get; set; }

    [JsonPropertyName("segment")]
    public int Segment { get; set; }

    [JsonPropertyName("smsCharacters")]
    public int SmsCharacters { get; set; }
}
