using System.Threading;
using System.Threading.Tasks;
using GoSMSCore.Models;

namespace GoSMSCore;

public interface IGoSmsClient
{
    Task<SmsSendResponse> SendAsync(string phoneNumber, string text, bool urgent = false, CancellationToken ct = default);
    Task<SendBulkSmsResponse> SendBulkAsync(string[] phoneNumbers, string text, bool urgent = false, string? noSmsNumber = null, CancellationToken ct = default);
    Task<OtpSendResponse> SendOtpAsync(string phoneNumber, CancellationToken ct = default);
    Task<OtpVerifyResponse> VerifyOtpAsync(string phoneNumber, string hash, string code, CancellationToken ct = default);
    Task<CheckStatusResponse> CheckStatusAsync(int messageId, CancellationToken ct = default);
    Task<BalanceResponse> CheckBalanceAsync(CancellationToken ct = default);
    Task<SenderCreateResponse> CreateSenderAsync(string name, CancellationToken ct = default);
}
