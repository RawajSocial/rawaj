using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Auth.Login;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUnknownEmail_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
            .Returns((ApplicationUserDto?)null);

        var handler = new LoginCommandHandler(identityService, Substitute.For<IJwtTokenGenerator>(), dbContext);

        var result = await handler.Handle(new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUserDto { Id = Guid.NewGuid(), Email = "u@example.com", FullName = "U", IsActive = false };

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new LoginCommandHandler(identityService, Substitute.For<IJwtTokenGenerator>(), dbContext);

        var result = await handler.Handle(new LoginCommand(user.Email, "whatever"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUserDto { Id = Guid.NewGuid(), Email = "u@example.com", FullName = "U", IsActive = true };

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        identityService.CheckPasswordAsync(user.Id, "wrong", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new LoginCommandHandler(identityService, Substitute.For<IJwtTokenGenerator>(), dbContext);

        var result = await handler.Handle(new LoginCommand(user.Email, "wrong"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUserDto { Id = Guid.NewGuid(), Email = "u@example.com", FullName = "U", IsActive = true };

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        identityService.CheckPasswordAsync(user.Id, "correct", Arg.Any<CancellationToken>()).Returns(true);

        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.GenerateToken(user).Returns("fake-access-token");

        var handler = new LoginCommandHandler(identityService, jwtTokenGenerator, dbContext);

        var result = await handler.Handle(new LoginCommand(user.Email, "correct"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("fake-access-token", result.Data!.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.Single(dbContext.RefreshTokens);
        await identityService.Received(1).UpdateLastLoginAsync(user.Id, Arg.Any<CancellationToken>());
    }
}
