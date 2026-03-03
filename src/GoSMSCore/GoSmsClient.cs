using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoSMSCore.Exceptions;
using GoSMSCore.Models;
using Microsoft.Extensions.Options;

namespace GoSMSCore;

public class GoSmsClient : IGoSmsClient
{
    private readonly HttpClient _httpClient;
    private readonly GoSmsClientOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GoSmsClient(HttpClient httpClient, IOptions<GoSmsClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<SmsSendResponse> SendAsync(string phoneNumber, string text, bool urgent = false, CancellationToken ct = default)
    {
        if (phoneNumber is null) throw new ArgumentNullException(nameof(phoneNumber));
        if (text is null) throw new ArgumentNullException(nameof(text));

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["from"] = _options.Sender,
            ["to"] = phoneNumber,
            ["text"] = text,
            ["urgent"] = urgent
        };

        var (result, _) = await PostAsync<SmsSendResponse>("/sendsms", payload, ct);
        return result;
    }

    public async Task<SendBulkSmsResponse> SendBulkAsync(string[] phoneNumbers, string text, bool urgent = false, string? noSmsNumber = null, CancellationToken ct = default)
    {
        if (phoneNumbers is null) throw new ArgumentNullException(nameof(phoneNumbers));
        if (text is null) throw new ArgumentNullException(nameof(text));

        if (phoneNumbers.Length == 0)
            throw new ArgumentException("At least one phone number is required.", nameof(phoneNumbers));

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["from"] = _options.Sender,
            ["to"] = phoneNumbers,
            ["text"] = text,
            ["urgent"] = urgent
        };

        if (noSmsNumber != null)
            payload["noSmsNumber"] = noSmsNumber;

        var (result, _) = await PostAsync<SendBulkSmsResponse>("/sendbulk", payload, ct);
        return result;
    }

    public async Task<OtpSendResponse> SendOtpAsync(string phoneNumber, CancellationToken ct = default)
    {
        if (phoneNumber is null) throw new ArgumentNullException(nameof(phoneNumber));

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["phone"] = phoneNumber
        };

        var (result, headers) = await PostAsync<OtpSendResponse>("/otp/send", payload, ct);
        result.RateLimitInfo = RateLimitInfo.FromHeaders(headers);
        return result;
    }

    public async Task<OtpVerifyResponse> VerifyOtpAsync(string phoneNumber, string hash, string code, CancellationToken ct = default)
    {
        if (phoneNumber is null) throw new ArgumentNullException(nameof(phoneNumber));
        if (hash is null) throw new ArgumentNullException(nameof(hash));
        if (code is null) throw new ArgumentNullException(nameof(code));

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["phone"] = phoneNumber,
            ["hash"] = hash,
            ["code"] = code
        };

        var (result, headers) = await PostAsync<OtpVerifyResponse>("/otp/verify", payload, ct);
        result.RateLimitInfo = RateLimitInfo.FromHeaders(headers);
        return result;
    }

    public async Task<CheckStatusResponse> CheckStatusAsync(int messageId, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["messageId"] = messageId
        };

        var (result, _) = await PostAsync<CheckStatusResponse>("/checksms", payload, ct);
        return result;
    }

    public async Task<BalanceResponse> CheckBalanceAsync(CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey
        };

        var (result, _) = await PostAsync<BalanceResponse>("/sms-balance", payload, ct);
        return result;
    }

    public async Task<SenderCreateResponse> CreateSenderAsync(string name, CancellationToken ct = default)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _options.ApiKey,
            ["name"] = name
        };

        var (result, _) = await PostAsync<SenderCreateResponse>("/sender", payload, ct);
        return result;
    }

    private async Task<(TResponse, System.Net.Http.Headers.HttpResponseHeaders)> PostAsync<TResponse>(string path, Dictionary<string, object> payload, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, payload, JsonOptions, ct);
        var headers = response.Headers;

#if NETSTANDARD2_0
        var content = await response.Content.ReadAsStringAsync();
#else
        var content = await response.Content.ReadAsStringAsync(ct);
#endif

        if (!response.IsSuccessStatusCode)
        {
            GoSmsApiError? error = null;
            try
            {
                error = JsonSerializer.Deserialize<GoSmsApiError>(content, JsonOptions);
            }
            catch (JsonException)
            {
                // Response body wasn't valid JSON error
            }

            if (error != null && error.ErrorCode > 0)
            {
                int? retryAfter = ExtractRetryAfter(headers);
                throw GoSmsApiException.FromApiError(error, retryAfter);
            }

            response.EnsureSuccessStatusCode();
        }

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);

        if (result == null)
        {
            throw new GoSmsApiException(0, "Failed to deserialize API response");
        }

        // Check for error in successful HTTP responses (API returns 200 with error body)
        var errorCheck = JsonSerializer.Deserialize<GoSmsApiError>(content, JsonOptions);
        if (errorCheck != null && errorCheck.ErrorCode > 0)
        {
            int? retryAfter = ExtractRetryAfter(headers);
            throw GoSmsApiException.FromApiError(errorCheck, retryAfter);
        }

        return (result, headers);
    }

    private static int? ExtractRetryAfter(System.Net.Http.Headers.HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("Retry-After", out var values))
        {
            if (int.TryParse(values.FirstOrDefault(), out var seconds))
                return seconds;
        }
        return null;
    }
}
