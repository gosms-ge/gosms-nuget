using System;
using GoSMSCore.Models;

namespace GoSMSCore.Exceptions;

public class GoSmsApiException : Exception
{
    public int ErrorCode { get; }
    public string ErrorMessage { get; }
    public int? RetryAfter { get; }

    public GoSmsApiException(int errorCode, string errorMessage, int? retryAfter = null)
        : base($"GoSMS API error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RetryAfter = retryAfter;
    }

    public static GoSmsApiException FromApiError(GoSmsApiError error, int? retryAfter = null)
    {
        return new GoSmsApiException(error.ErrorCode, error.Message ?? "Unknown error", retryAfter);
    }
}
