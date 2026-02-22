using System;

namespace GoSMSCore;

public class GoSmsClientOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.gosms.ge/api";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
