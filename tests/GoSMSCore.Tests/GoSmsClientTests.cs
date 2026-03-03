using System.Net;
using System.Text;
using System.Text.Json;
using GoSMSCore;
using GoSMSCore.Exceptions;
using GoSMSCore.Models;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace GoSMSCore.Tests;

public class GoSmsClientTests
{
    private readonly GoSmsClientOptions _options = new()
    {
        ApiKey = "test-api-key",
        Sender = "TestBrand",
        BaseUrl = "https://api.gosms.ge/api"
    };

    private GoSmsClient CreateClient(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };

        var options = Options.Create(_options);
        return new GoSmsClient(httpClient, options);
    }

    private static HttpResponseMessage JsonResponse(
        object body,
        HttpStatusCode status = HttpStatusCode.OK,
        Dictionary<string, string>? headers = null)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (headers != null)
        {
            foreach (var kvp in headers)
                response.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        return response;
    }

    #region SendAsync

    [Fact]
    public async Task SendAsync_Success_ReturnsResponse()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            messageId = 12345,
            from = "TestBrand",
            to = "995555123456",
            text = "Hello",
            sendAt = "2025-01-15T10:30:00.000Z",
            balance = 100,
            encode = "default",
            segment = 1,
            smsCharacters = 5
        }));

        var result = await client.SendAsync("995555123456", "Hello");

        Assert.True(result.Success);
        Assert.Equal(12345, result.MessageId);
        Assert.Equal("995555123456", result.To);
        Assert.Equal("Hello", result.Text);
        Assert.Equal(100, result.Balance);
    }

    [Fact]
    public async Task SendAsync_NullPhone_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync(null!, "text"));
    }

    [Fact]
    public async Task SendAsync_NullText_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAsync("995555123456", null!));
    }

    #endregion

    #region SendBulkAsync

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsResponse()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            totalCount = 2,
            successCount = 2,
            failedCount = 0,
            balance = 98,
            from = "TestBrand",
            text = "Bulk hello",
            encode = "default",
            segment = 1,
            smsCharacters = 10,
            messages = new[]
            {
                new { messageId = 101, to = "995555111111", success = true, error = (string?)null },
                new { messageId = 102, to = "995555222222", success = true, error = (string?)null }
            }
        }));

        var result = await client.SendBulkAsync(new[] { "995555111111", "995555222222" }, "Bulk hello");

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.NotNull(result.Messages);
        Assert.Equal(2, result.Messages!.Count);
        Assert.Equal(101, result.Messages[0].MessageId);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyArray_ThrowsArgument()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendBulkAsync(Array.Empty<string>(), "text"));
    }

    [Fact]
    public async Task SendBulkAsync_NullArray_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendBulkAsync(null!, "text"));
    }

    #endregion

    #region SendOtpAsync

    [Fact]
    public async Task SendOtpAsync_Success_ReturnsResponse()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            hash = "abc123hash",
            balance = 99,
            to = "995555123456",
            sendAt = "2025-01-15T10:30:00.000Z",
            encode = "default",
            segment = 1,
            smsCharacters = 6
        }));

        var result = await client.SendOtpAsync("995555123456");

        Assert.True(result.Success);
        Assert.Equal("abc123hash", result.Hash);
        Assert.Equal(99, result.Balance);
    }

    [Fact]
    public async Task SendOtpAsync_NullPhone_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendOtpAsync(null!));
    }

    #endregion

    #region VerifyOtpAsync

    [Fact]
    public async Task VerifyOtpAsync_Success_ReturnsVerified()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            verify = true
        }));

        var result = await client.VerifyOtpAsync("995555123456", "abc123hash", "1234");

        Assert.True(result.Success);
        Assert.True(result.Verify);
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidCode_ReturnsFalse()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            verify = false
        }));

        var result = await client.VerifyOtpAsync("995555123456", "abc123hash", "0000");

        Assert.True(result.Success);
        Assert.False(result.Verify);
    }

    [Fact]
    public async Task VerifyOtpAsync_NullParams_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.VerifyOtpAsync(null!, "hash", "code"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.VerifyOtpAsync("phone", null!, "code"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.VerifyOtpAsync("phone", "hash", null!));
    }

    #endregion

    #region CheckStatusAsync

    [Fact]
    public async Task CheckStatusAsync_Success_ReturnsStatus()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            messageId = 12345,
            from = "TestBrand",
            to = "995555123456",
            text = "Hello",
            sendAt = "2025-01-15T10:30:00.000Z",
            encode = "default",
            segment = 1,
            smsCharacters = 5,
            status = "delivered"
        }));

        var result = await client.CheckStatusAsync(12345);

        Assert.True(result.Success);
        Assert.Equal(12345, result.MessageId);
        Assert.Equal("delivered", result.Status);
    }

    #endregion

    #region CheckBalanceAsync

    [Fact]
    public async Task CheckBalanceAsync_Success_ReturnsBalance()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            balance = 500
        }));

        var result = await client.CheckBalanceAsync();

        Assert.True(result.Success);
        Assert.Equal(500, result.Balance);
    }

    #endregion

    #region CreateSenderAsync

    [Fact]
    public async Task CreateSenderAsync_Success_ReturnsTrue()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true
        }));

        var result = await client.CreateSenderAsync("NewBrand");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateSenderAsync_NullName_ThrowsArgumentNull()
    {
        var client = CreateClient(JsonResponse(new { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateSenderAsync(null!));
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ApiError_InResponseBody_ThrowsGoSmsApiException()
    {
        var client = CreateClient(JsonResponse(new
        {
            errorCode = 100,
            message = "Invalid API key"
        }, HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<GoSmsApiException>(() => client.CheckBalanceAsync());
        Assert.Equal(100, ex.ErrorCode);
        Assert.Equal("Invalid API key", ex.ErrorMessage);
    }

    [Fact]
    public async Task ApiError_InSuccessResponse_ThrowsGoSmsApiException()
    {
        var client = CreateClient(JsonResponse(new
        {
            errorCode = 102,
            message = "Insufficient balance"
        }));

        var ex = await Assert.ThrowsAsync<GoSmsApiException>(() => client.SendAsync("995555123456", "Hello"));
        Assert.Equal(102, ex.ErrorCode);
    }

    [Fact]
    public async Task HttpError_NoJsonBody_ThrowsHttpRequestException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error", Encoding.UTF8, "text/plain")
        };
        var client = CreateClient(response);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CheckBalanceAsync());
    }

    [Fact]
    public async Task CancellationToken_Respected()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        var client = new GoSmsClient(httpClient, Options.Create(_options));

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.CheckBalanceAsync(cts.Token));
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GoSmsClient(null!, Options.Create(_options)));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.gosms.ge/api") };
        Assert.Throws<ArgumentNullException>(() => new GoSmsClient(httpClient, null!));
    }

    #endregion

    #region Request Payload Verification

    [Fact]
    public async Task SendAsync_SendsCorrectPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(JsonResponse(new { success = true, messageId = 1, balance = 100 }));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        var client = new GoSmsClient(httpClient, Options.Create(_options));

        await client.SendAsync("995555123456", "Hello", urgent: true);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Contains("/sendsms", capturedRequest.RequestUri!.PathAndQuery);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("test-api-key", root.GetProperty("api_key").GetString());
        Assert.Equal("TestBrand", root.GetProperty("from").GetString());
        Assert.Equal("995555123456", root.GetProperty("to").GetString());
        Assert.Equal("Hello", root.GetProperty("text").GetString());
        Assert.True(root.GetProperty("urgent").GetBoolean());
    }

    [Fact]
    public async Task SendBulkAsync_IncludesNoSmsNumber_WhenProvided()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(JsonResponse(new { success = true, totalCount = 1, successCount = 1, failedCount = 0, balance = 99, messages = new[] { new { messageId = 1, to = "995555111111", success = true } } }));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        var client = new GoSmsClient(httpClient, Options.Create(_options));

        await client.SendBulkAsync(new[] { "995555111111" }, "test", noSmsNumber: "995555999999");

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("995555999999", doc.RootElement.GetProperty("noSmsNumber").GetString());
    }

    [Fact]
    public async Task SendBulkAsync_OmitsNoSmsNumber_WhenNull()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(JsonResponse(new { success = true, totalCount = 1, successCount = 1, failedCount = 0, balance = 99, messages = new[] { new { messageId = 1, to = "995555111111", success = true } } }));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        var client = new GoSmsClient(httpClient, Options.Create(_options));

        await client.SendBulkAsync(new[] { "995555111111" }, "test");

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.TryGetProperty("noSmsNumber", out _));
    }

    #endregion

    #region Rate Limit Headers

    [Fact]
    public async Task SendOtpAsync_WithRateLimitHeaders_ReturnsRateLimitInfo()
    {
        var response = JsonResponse(
            new { success = true, hash = "abc123", balance = 99, to = "995555123456", sendAt = "", encode = "default", segment = 1, smsCharacters = 6 },
            headers: new Dictionary<string, string>
            {
                ["X-RateLimit-Limit"] = "10",
                ["X-RateLimit-Remaining"] = "7"
            });
        var client = CreateClient(response);

        var result = await client.SendOtpAsync("995555123456");

        Assert.NotNull(result.RateLimitInfo);
        Assert.Equal(10, result.RateLimitInfo!.Limit);
        Assert.Equal(7, result.RateLimitInfo.Remaining);
        Assert.Null(result.RateLimitInfo.RetryAfter);
    }

    [Fact]
    public async Task SendOtpAsync_WithoutRateLimitHeaders_ReturnsNullRateLimitInfo()
    {
        var client = CreateClient(JsonResponse(new
        {
            success = true,
            hash = "abc123",
            balance = 99,
            to = "995555123456",
            sendAt = "",
            encode = "default",
            segment = 1,
            smsCharacters = 6
        }));

        var result = await client.SendOtpAsync("995555123456");

        Assert.Null(result.RateLimitInfo);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithRateLimitHeaders_ReturnsRateLimitInfo()
    {
        var response = JsonResponse(
            new { success = true, verify = true },
            headers: new Dictionary<string, string>
            {
                ["X-RateLimit-Limit"] = "5",
                ["X-RateLimit-Remaining"] = "3",
                ["Retry-After"] = "60"
            });
        var client = CreateClient(response);

        var result = await client.VerifyOtpAsync("995555123456", "hash", "1234");

        Assert.NotNull(result.RateLimitInfo);
        Assert.Equal(5, result.RateLimitInfo!.Limit);
        Assert.Equal(3, result.RateLimitInfo.Remaining);
        Assert.Equal(60, result.RateLimitInfo.RetryAfter);
    }

    [Fact]
    public async Task ApiError_WithRetryAfter_SetsRetryAfterOnException()
    {
        var response = JsonResponse(
            new { errorCode = 109, message = "Too many requests" },
            HttpStatusCode.TooManyRequests,
            new Dictionary<string, string> { ["Retry-After"] = "120" });
        var client = CreateClient(response);

        var ex = await Assert.ThrowsAsync<GoSmsApiException>(() => client.SendOtpAsync("995555123456"));
        Assert.Equal(109, ex.ErrorCode);
        Assert.Equal(120, ex.RetryAfter);
    }

    [Fact]
    public async Task ApiError_WithoutRetryAfter_RetryAfterIsNull()
    {
        var response = JsonResponse(
            new { errorCode = 100, message = "Invalid API key" },
            HttpStatusCode.Unauthorized);
        var client = CreateClient(response);

        var ex = await Assert.ThrowsAsync<GoSmsApiException>(() => client.CheckBalanceAsync());
        Assert.Equal(100, ex.ErrorCode);
        Assert.Null(ex.RetryAfter);
    }

    #endregion
}
