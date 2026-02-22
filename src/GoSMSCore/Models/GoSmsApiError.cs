using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class GoSmsApiError
{
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
