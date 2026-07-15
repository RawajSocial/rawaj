using Rawaj.Application.Common.Policies;
using Xunit;

namespace Rawaj.Application.Tests.Policies;

public class RefreshTokenPolicyTests
{
    [Fact]
    public async Task Issue_CreatesARetrievableToken()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        var rawToken = RefreshTokenPolicy.Issue(dbContext, userId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(rawToken));
        Assert.Single(dbContext.RefreshTokens);
        Assert.Equal(userId, dbContext.RefreshTokens.Single().UserId);
    }

    [Fact]
    public async Task RotateAsync_WithValidToken_RevokesOldAndIssuesNew()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var rawToken = RefreshTokenPolicy.Issue(dbContext, userId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await RefreshTokenPolicy.RotateAsync(dbContext, rawToken, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Data.UserId);
        Assert.NotEqual(rawToken, result.Data.NewRawToken);

        var original = dbContext.RefreshTokens.Single(t => t.UserId == userId && t.RevokedAt != null);
        Assert.NotNull(original.RevokedAt);
    }

    [Fact]
    public async Task RotateAsync_ReusingAnAlreadyRotatedToken_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var rawToken = RefreshTokenPolicy.Issue(dbContext, userId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await RefreshTokenPolicy.RotateAsync(dbContext, rawToken, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var secondAttempt = await RefreshTokenPolicy.RotateAsync(dbContext, rawToken, CancellationToken.None);

        Assert.False(secondAttempt.Succeeded);
    }

    [Fact]
    public async Task RotateAsync_WithUnknownToken_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();

        var result = await RefreshTokenPolicy.RotateAsync(dbContext, "does-not-exist", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [Fact]
    public async Task RevokeAsync_WithValidToken_MarksItRevokedAndReturnsTrue()
    {
        using var dbContext = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var rawToken = RefreshTokenPolicy.Issue(dbContext, userId);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var revoked = await RefreshTokenPolicy.RevokeAsync(dbContext, rawToken, CancellationToken.None);

        Assert.True(revoked);
        Assert.NotNull(dbContext.RefreshTokens.Single().RevokedAt);
    }

    [Fact]
    public async Task RevokeAsync_WithUnknownToken_ReturnsFalse()
    {
        using var dbContext = TestDbContextFactory.Create();

        var revoked = await RefreshTokenPolicy.RevokeAsync(dbContext, "does-not-exist", CancellationToken.None);

        Assert.False(revoked);
    }
}
