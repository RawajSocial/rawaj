using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Features.Brands.CreateBrandProfile;
using Rawaj.Application.Features.Brands.GenerateOnboardingQuestions;
using Rawaj.Application.Features.Brands.GetBrandProfiles;
using Rawaj.Application.Features.Brands.UpdateBrandProfile;
using Rawaj.Common;
using Rawaj.Domain.Enums;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/brand-profiles")]
public class BrandProfilesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<CreateBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<CreateBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrandProfilesQuery(), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<BrandProfileSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<BrandProfileSummary>>.Fail(result.ErrorMessage!));
    }

    public record UpdateBrandProfileRequest(
        string? Name,
        string? Description,
        BrandVoice? BrandVoice,
        string? Tagline,
        string? Industry,
        string? TargetAudience,
        List<string>? Colors,
        string? LogoUrl,
        string? WebsiteUrl,
        List<string>? SupportedLanguages,
        List<string>? Keywords);

    [HttpPut("{brandProfileId:guid}")]
    public async Task<IActionResult> Update(Guid brandProfileId, UpdateBrandProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateBrandProfileCommand(
            brandProfileId,
            request.Name,
            request.Description,
            request.BrandVoice,
            request.Tagline,
            request.Industry,
            request.TargetAudience,
            request.Colors,
            request.LogoUrl,
            request.WebsiteUrl,
            request.SupportedLanguages,
            request.Keywords);

        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<UpdateBrandProfileResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<UpdateBrandProfileResponse>.Fail(result.ErrorMessage!));
    }

    public record GenerateOnboardingQuestionsRequest(JsonElement OnboardingContext);

    [HttpPost("{brandProfileId:guid}/onboarding-questions")]
    public async Task<IActionResult> GenerateOnboardingQuestions(
        Guid brandProfileId, GenerateOnboardingQuestionsRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateOnboardingQuestionsCommand(brandProfileId, request.OnboardingContext.GetRawText());

        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<GenerateOnboardingQuestionsResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<GenerateOnboardingQuestionsResponse>.Fail(result.ErrorMessage!));
    }
}
