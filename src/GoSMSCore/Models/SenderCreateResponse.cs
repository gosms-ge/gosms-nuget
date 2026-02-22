using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class SenderCreateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
