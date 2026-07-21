using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Auth.Register;
using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingEmail_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var existing = new ApplicationUserDto { Id = Guid.NewGuid(), Email = "taken@example.com", FullName = "X", IsActive = true };

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(existing.Email, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new RegisterCommandHandler(identityService, Substitute.For<IJwtTokenGenerator>(), dbContext);

        var result = await handler.Handle(
            new RegisterCommand(existing.Email, "P@ssw0rd1", "New Person", Language.En), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("A user with this email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenIdentityCreationFails_ReturnsJoinedErrors()
    {
        using var dbContext = TestDbContextFactory.Create();
        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(IdentityRegisterResult.Failure(["Password too weak."]));

        var handler = new RegisterCommandHandler(identityService, Substitute.For<IJwtTokenGenerator>(), dbContext);

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "weak", "New Person", Language.En), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Password too weak.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithValidInput_CreatesUserAndIssuesTokens()
    {
        using var dbContext = TestDbContextFactory.Create();
        var newUserId = Guid.NewGuid();

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.CreateUserAsync("new@example.com", "P@ssw0rd1", "New Person", Language.En, Arg.Any<CancellationToken>())
            .Returns(IdentityRegisterResult.Success(newUserId));

        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.GenerateToken(Arg.Any<ApplicationUserDto>()).Returns("fake-access-token");

        var handler = new RegisterCommandHandler(identityService, jwtTokenGenerator, dbContext);

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "P@ssw0rd1", "New Person", Language.En), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(newUserId, result.Data!.UserId);
        Assert.Equal("fake-access-token", result.Data.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.Single(dbContext.RefreshTokens);
    }
}
