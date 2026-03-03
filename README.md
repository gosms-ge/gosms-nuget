[![NuGet Version](https://img.shields.io/nuget/v/GoSMSCore.svg)](https://www.nuget.org/packages/GoSMSCore/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/GoSMSCore.svg)](https://www.nuget.org/packages/GoSMSCore/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Release](https://github.com/gosms-ge/gosms-nuget/actions/workflows/release.yml/badge.svg)](https://github.com/gosms-ge/gosms-nuget/actions/workflows/release.yml)

# GoSMSCore — .NET SDK for GOSMS.GE

.NET client library to send SMS messages using the [GOSMS.GE](https://gosms.ge) SMS Gateway.

**Targets:** .NET Standard 2.0 (Framework 4.6.1+, .NET Core 2.0+) and .NET 8.0

To use this library, you must have a valid account on https://gosms.ge.

**Please note** SMS messages sent with this library will be deducted from your GOSMS.GE account credits.

For any questions, please contact us at info@gosms.ge

## Installation

```bash
dotnet add package GoSMSCore
```

Or via the NuGet Package Manager:

```
Install-Package GoSMSCore
```

## Quick Start

### With Dependency Injection (recommended)

```csharp
// In Program.cs or Startup.cs
builder.Services.AddGoSmsClient(options =>
{
    options.ApiKey = "your_api_key";
    options.Sender = "GOSMS.GE";
});
```

```csharp
// In your service or controller
public class SmsNotificationService
{
    private readonly IGoSmsClient _sms;

    public SmsNotificationService(IGoSmsClient sms)
    {
        _sms = sms;
    }

    public async Task SendWelcomeMessage(string phone)
    {
        var result = await _sms.SendAsync(phone, "Welcome to our service!");
        Console.WriteLine($"Message ID: {result.MessageId}, Balance: {result.Balance}");
    }
}
```

### Without Dependency Injection

```csharp
using GoSMSCore;
using Microsoft.Extensions.Options;

var options = Options.Create(new GoSmsClientOptions
{
    ApiKey = "your_api_key",
    Sender = "GOSMS.GE"
});

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.gosms.ge/api") };
var sms = new GoSmsClient(httpClient, options);

var result = await sms.SendAsync("995555555555", "Hello!");
Console.WriteLine($"Message ID: {result.MessageId}");
```

## Usage

### Send SMS

```csharp
var result = await sms.SendAsync("995555555555", "Hello!");
Console.WriteLine($"Message ID: {result.MessageId}");
Console.WriteLine($"Balance: {result.Balance}");

// Send urgent message
var urgent = await sms.SendAsync("995555555555", "Urgent alert!", urgent: true);
```

### Send Bulk SMS

```csharp
var phones = new[] { "995555111111", "995555222222", "995555333333" };
var result = await sms.SendBulkAsync(phones, "Hello everyone!");

Console.WriteLine($"Sent: {result.SuccessCount}/{result.TotalCount}");
foreach (var msg in result.Messages!)
{
    if (!msg.Success)
        Console.WriteLine($"Failed: {msg.To} - {msg.Error}");
}

// With noSmsNumber for advertising campaigns
var adResult = await sms.SendBulkAsync(phones, "Special offer!", noSmsNumber: "995322000000");
```

### Send OTP

```csharp
var otpResult = await sms.SendOtpAsync("995555555555");
Console.WriteLine($"Hash: {otpResult.Hash}"); // Save this for verification
Console.WriteLine($"Balance: {otpResult.Balance}");

// Rate limit info
if (otpResult.RateLimitInfo != null)
{
    Console.WriteLine($"Remaining: {otpResult.RateLimitInfo.Remaining}");
    Console.WriteLine($"Limit: {otpResult.RateLimitInfo.Limit}");
}
```

### Verify OTP

```csharp
var verifyResult = await sms.VerifyOtpAsync("995555555555", "hash_from_sendOtp", "1234");
if (verifyResult.Verify)
    Console.WriteLine("OTP verified successfully");
else
    Console.WriteLine("Invalid OTP code");

// Rate limit info
if (verifyResult.RateLimitInfo != null)
{
    Console.WriteLine($"Remaining: {verifyResult.RateLimitInfo.Remaining}");
}
```

> **Note:** A wrong OTP code returns `Verify = false` without throwing an exception.
> Exceptions are thrown only for expired OTPs, already-used OTPs, and locked accounts.

### Check Message Status

```csharp
var status = await sms.CheckStatusAsync(12345);
Console.WriteLine($"Status: {status.Status}"); // e.g., "delivered", "pending", "failed"
Console.WriteLine($"From: {status.From}");
Console.WriteLine($"To: {status.To}");
```

### Check Balance

```csharp
var balance = await sms.CheckBalanceAsync();
Console.WriteLine($"Balance: {balance.Balance}");
```

### Create Sender Name

```csharp
var sender = await sms.CreateSenderAsync("MyCompany");
if (sender.Success)
    Console.WriteLine("Sender name created. Wait for approval before using it.");
```

## Error Handling

The SDK throws `GoSmsApiException` for API errors with typed error codes:

```csharp
using GoSMSCore;
using GoSMSCore.Exceptions;

try
{
    var result = await sms.SendAsync("995555555555", "Hello!");
}
catch (GoSmsApiException ex) when (ex.ErrorCode == GoSmsErrorCode.InsufficientBalance)
{
    Console.WriteLine("Not enough credits!");
}
catch (GoSmsApiException ex)
{
    Console.WriteLine($"API error {ex.ErrorCode}: {ex.ErrorMessage}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
```

### Rate Limit Errors

When OTP rate limits are exceeded, the exception includes `RetryAfter` (seconds until lockout expires):

```csharp
try
{
    var result = await sms.SendOtpAsync("995555555555");
}
catch (GoSmsApiException ex) when (ex.ErrorCode == GoSmsErrorCode.TooManyRequests)
{
    Console.WriteLine($"Too many attempts. Retry after {ex.RetryAfter}s");
}
catch (GoSmsApiException ex) when (ex.ErrorCode == GoSmsErrorCode.AccountLocked)
{
    Console.WriteLine($"Account locked. Retry after {ex.RetryAfter}s");
}
```

## Configuration

```csharp
builder.Services.AddGoSmsClient(options =>
{
    options.ApiKey = "your_api_key";       // Required
    options.Sender = "GOSMS.GE";           // Required - registered sender name
    options.BaseUrl = "https://api.gosms.ge/api"; // Optional (default)
    options.Timeout = TimeSpan.FromSeconds(30);   // Optional (default: 30s)
});
```

## API Reference

### `IGoSmsClient` Interface

| Method | Description |
|--------|-------------|
| `SendAsync(phoneNumber, text, urgent?, ct?)` | Send SMS to a single recipient |
| `SendBulkAsync(phoneNumbers, text, urgent?, noSmsNumber?, ct?)` | Send SMS to multiple recipients (max 1000) |
| `SendOtpAsync(phoneNumber, ct?)` | Send OTP code |
| `VerifyOtpAsync(phoneNumber, hash, code, ct?)` | Verify OTP code |
| `CheckStatusAsync(messageId, ct?)` | Check message delivery status |
| `CheckBalanceAsync(ct?)` | Check account SMS balance |
| `CreateSenderAsync(name, ct?)` | Register a new sender name |

All methods accept an optional `CancellationToken` parameter.

### Response Types

#### `SmsSendResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the message was sent |
| `MessageId` | `int` | Unique message identifier |
| `From` | `string?` | Sender name |
| `To` | `string?` | Recipient phone number |
| `Text` | `string?` | Message text |
| `Balance` | `int` | Remaining SMS credits |
| `SendAt` | `string?` | Send timestamp (ISO 8601) |
| `Encode` | `string?` | Message encoding |
| `Segment` | `int` | Number of SMS segments |
| `SmsCharacters` | `int` | Character count |

#### `SendBulkSmsResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the bulk send succeeded |
| `TotalCount` | `int` | Total recipients |
| `SuccessCount` | `int` | Successfully sent count |
| `FailedCount` | `int` | Failed count |
| `Balance` | `int` | Remaining SMS credits |
| `Messages` | `List<BulkSmsResult>?` | Per-recipient results |

#### `BulkSmsResult`

| Property | Type | Description |
|----------|------|-------------|
| `MessageId` | `int` | Message identifier |
| `To` | `string?` | Recipient phone number |
| `Success` | `bool` | Whether this message was sent |
| `Error` | `string?` | Error message if failed |

#### `OtpSendResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether OTP was sent |
| `Hash` | `string?` | Hash for verification |
| `Balance` | `int` | Remaining SMS credits |
| `To` | `string?` | Recipient phone number |
| `SendAt` | `string?` | Send timestamp (ISO 8601) |
| `RateLimitInfo` | `RateLimitInfo?` | Rate limit info (OTP endpoints only) |

#### `OtpVerifyResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the request succeeded |
| `Verify` | `bool` | Whether the OTP code is valid |
| `RateLimitInfo` | `RateLimitInfo?` | Rate limit info (OTP endpoints only) |

#### `RateLimitInfo`

| Property | Type | Description |
|----------|------|-------------|
| `Limit` | `int?` | Maximum requests allowed in the window |
| `Remaining` | `int?` | Requests remaining in the window |
| `RetryAfter` | `int?` | Seconds until lockout expires |

#### `CheckStatusResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the request succeeded |
| `MessageId` | `int` | Message identifier |
| `Status` | `string?` | Delivery status |
| `From` | `string?` | Sender name |
| `To` | `string?` | Recipient phone number |
| `Text` | `string?` | Message text |
| `SendAt` | `string?` | Send timestamp (ISO 8601) |

#### `BalanceResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the request succeeded |
| `Balance` | `int` | Remaining SMS credits |

#### `SenderCreateResponse`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the sender was created |

### Error Codes (`GoSmsErrorCode`)

| Constant | Code | Description |
|----------|------|-------------|
| `InvalidApiKey` | 100 | Invalid API key |
| `InvalidSender` | 101 | Invalid or unapproved sender name |
| `InsufficientBalance` | 102 | Not enough SMS credits |
| `InvalidParameters` | 103 | Invalid request parameters |
| `MessageNotFound` | 104 | Message ID not found |
| `InvalidPhone` | 105 | Invalid phone number |
| `OtpFailed` | 106 | OTP sending failed |
| `SenderExists` | 107 | Sender name already exists |
| `NotConfigured` | 108 | Account not configured |
| `TooManyRequests` | 109 | Rate limit exceeded |
| `AccountLocked` | 110 | Account is locked |
| `OtpExpired` | 111 | OTP code has expired |
| `OtpAlreadyUsed` | 112 | OTP code already used |
| `InvalidNoSmsNumber` | 113 | Invalid NoSMS number |

## Testing

```bash
dotnet test
```

The test suite uses xUnit and Moq to cover all 7 API endpoints, input validation, error handling, and request payload verification.

## Versioning & Releases

This project follows [Semantic Versioning](https://semver.org/) with automated releases via GitHub Actions.

### Commit Convention

We use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` — New features (MINOR version bump)
- `fix:` — Bug fixes (PATCH version bump)
- `chore:`, `docs:`, `refactor:`, `ci:`, etc. — Other changes (PATCH version bump)
- `BREAKING CHANGE:` — Breaking changes (MAJOR version bump)

### Automated Releases

Releases are automatically published when commits are pushed to `main`:

1. Build and test on .NET 8.0
2. Analyze commits for version bump type
3. Update version in `.csproj`
4. Generate `CHANGELOG.md` entry
5. Create git tag `vX.Y.Z`
6. Pack and publish to NuGet.org
7. Create GitHub release

## Migration from v5.x

v6.0.0 is a full rewrite with breaking changes:

| v5.x | v6.0.0 |
|------|--------|
| `ISmsService` | `IGoSmsClient` |
| `AddGoSmsService()` | `AddGoSmsClient()` |
| `SendToOne()` / `SendToMany()` | `SendAsync()` / `SendBulkAsync()` |
| Sync methods (`.Result`) | Async-only with `CancellationToken` |
| Events (`OnSmsSent`, etc.) | `Task<T>` return values |
| `Message_Id` (string) | `MessageId` (int) |
| `SendAt` (DateTime) | `SendAt` (string, ISO 8601) |
| Newtonsoft.Json | System.Text.Json |
| Missing bulk SMS & sender | All 7 endpoints supported |

## More Info

Visit our website: https://www.gosms.ge
