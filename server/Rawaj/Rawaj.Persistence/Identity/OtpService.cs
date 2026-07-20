using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Identity;
using Rawaj.Domain.Enums;

namespace Rawaj.Persistence.Identity;

public class OtpService(
    AppDbContext dbContext,
    IIdentityService identityService,
    IEmailService emailService,
    IConfiguration configuration) : IOtpService
{
    public async Task<SendOtpResult> SendAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var maxSendsPerHour = configuration.GetValue<int?>("Otp:MaxSendsPerHour") ?? 5;
        var expiryMinutes = configuration.GetValue<int?>("Otp:ExpiryMinutes") ?? 10;

        var otp = await dbContext.EmailOtps.FirstOrDefaultAsync(o => o.UserId == userId && o.Purpose == purpose, cancellationToken);

        if (otp is null)
        {
            otp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Purpose = purpose,
                WindowStartAt = now,
                SendCount = 0,
                CreatedAt = now
            };
            dbContext.EmailOtps.Add(otp);
        }

        if (now - otp.WindowStartAt > TimeSpan.FromHours(1))
        {
            otp.WindowStartAt = now;
            otp.SendCount = 0;
        }

        if (otp.SendCount >= maxSendsPerHour)
        {
            return new SendOtpResult(SendOtpOutcome.RateLimited, otp.WindowStartAt.AddHours(1));
        }

        var user = await identityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new SendOtpResult(SendOtpOutcome.RateLimited, null);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        otp.CodeHash = Hash(code);
        otp.ExpiresAt = now.AddMinutes(expiryMinutes);
        otp.ConsumedAt = null;
        otp.SendCount++;
        otp.LastSentAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await SendOtpEmailAsync(user.Email, code, expiryMinutes, cancellationToken);

        return new SendOtpResult(SendOtpOutcome.Sent, null);
    }

    public async Task<VerifyOtpResult> VerifyAsync(Guid userId, OtpPurpose purpose, string code, CancellationToken cancellationToken)
    {
        var otp = await dbContext.EmailOtps.FirstOrDefaultAsync(o => o.UserId == userId && o.Purpose == purpose, cancellationToken);
        if (otp is null)
        {
            return new VerifyOtpResult(VerifyOtpOutcome.NotFound);
        }

        if (otp.ConsumedAt is not null)
        {
            return new VerifyOtpResult(VerifyOtpOutcome.AlreadyConsumed);
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            return new VerifyOtpResult(VerifyOtpOutcome.Expired);
        }

        if (otp.CodeHash != Hash(code))
        {
            return new VerifyOtpResult(VerifyOtpOutcome.Invalid);
        }

        otp.ConsumedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (purpose == OtpPurpose.EmailVerification)
        {
            await identityService.ConfirmEmailAsync(userId, cancellationToken);
        }

        return new VerifyOtpResult(VerifyOtpOutcome.Success);
    }

    private async Task SendOtpEmailAsync(string toEmail, string code, int expiryMinutes, CancellationToken cancellationToken)
    {
        var subject = "Your Rawaj verification code";

        var htmlBody = $"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 560px; margin: 0 auto; color: #1f2933;">
                <h2 style="color: #111827;">Verify your email</h2>
                <p>Your verification code is:</p>
                <p style="font-size: 32px; font-weight: bold; letter-spacing: 6px; margin: 24px 0;">{code}</p>
                <p>This code expires in {expiryMinutes} minutes. If you didn't request this, you can safely ignore this email.</p>
                <p style="margin-top: 32px; color: #6b7280; font-size: 13px;">— The Rawaj Team</p>
            </div>
            """;

        await emailService.SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    private string Hash(string code)
    {
        var pepper = configuration["Otp:Pepper"] ?? string.Empty;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code + pepper)));
    }
}
