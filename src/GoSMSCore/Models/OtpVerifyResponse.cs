using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class OtpVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("verify")]
    public bool Verify { get; set; }
}
