using System.Text.Json.Serialization;

namespace GoSMSCore.Models;

public class BalanceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("balance")]
    public int Balance { get; set; }
}
