using System;
using GoSMSCore.Models;

namespace GoSMSCore.Exceptions;

public class GoSmsApiException : Exception
{
    public int ErrorCode { get; }
    public string ErrorMessage { get; }

    public GoSmsApiException(int errorCode, string errorMessage)
        : base($"GoSMS API error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static GoSmsApiException FromApiError(GoSmsApiError error)
    {
        return new GoSmsApiException(error.ErrorCode, error.Message ?? "Unknown error");
    }
}
