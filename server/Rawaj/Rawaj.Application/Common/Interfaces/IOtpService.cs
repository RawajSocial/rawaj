using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

public interface IOtpService
{
    Task<SendOtpResult> SendAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken);

    Task<VerifyOtpResult> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken cancellationToken);
}
