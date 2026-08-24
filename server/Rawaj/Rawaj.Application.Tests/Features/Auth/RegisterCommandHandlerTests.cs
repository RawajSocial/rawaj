using NSubstitute;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Services;
using Rawaj.Application.Features.Auth.Register;
using Rawaj.Domain.Entities.Billing;
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

        var handler = new RegisterCommandHandler(
            identityService, Substitute.For<IJwtTokenGenerator>(), dbContext, new TenantProvisioningService(dbContext),
            Substitute.For<IEmailService>());

        var result = await handler.Handle(
            new RegisterCommand(existing.Email, "newperson", "P@ssw0rd1", "New Person", Language.En), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("A user with this email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithExistingUsername_Fails()
    {
        using var dbContext = TestDbContextFactory.Create();
        var existing = new ApplicationUserDto { Id = Guid.NewGuid(), Email = "taken@example.com", Username = "takenname", FullName = "X", IsActive = true };

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.FindByUsernameAsync(existing.Username, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new RegisterCommandHandler(
            identityService, Substitute.For<IJwtTokenGenerator>(), dbContext, new TenantProvisioningService(dbContext),
            Substitute.For<IEmailService>());

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", existing.Username, "P@ssw0rd1", "New Person", Language.En), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("A user with this username already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenIdentityCreationFails_ReturnsJoinedErrors()
    {
        using var dbContext = TestDbContextFactory.Create();
        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(IdentityRegisterResult.Failure(["Password too weak."]));

        var handler = new RegisterCommandHandler(
            identityService, Substitute.For<IJwtTokenGenerator>(), dbContext, new TenantProvisioningService(dbContext),
            Substitute.For<IEmailService>());

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "newperson", "weak", "New Person", Language.En), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Password too weak.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithValidInput_CreatesUserAndIssuesTokens()
    {
        using var dbContext = TestDbContextFactory.Create();
        dbContext.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Free",
            Cost = 0,
            Currency = "USD",
            MaxBrands = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var newUserId = Guid.NewGuid();

        var identityService = Substitute.For<IIdentityService>();
        identityService.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ApplicationUserDto?)null);
        identityService.CreateUserAsync("new@example.com", "newperson", "P@ssw0rd1", "New Person", Language.En, Arg.Any<CancellationToken>())
            .Returns(IdentityRegisterResult.Success(newUserId));

        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.GenerateToken(Arg.Any<ApplicationUserDto>()).Returns("fake-access-token");

        var handler = new RegisterCommandHandler(
            identityService, jwtTokenGenerator, dbContext, new TenantProvisioningService(dbContext),
            Substitute.For<IEmailService>());

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "newperson", "P@ssw0rd1", "New Person", Language.En), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(newUserId, result.Data!.UserId);
        Assert.Equal("fake-access-token", result.Data.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.Single(dbContext.RefreshTokens);
    }
}
