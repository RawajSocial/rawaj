using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Competitors.AddCompetitor;
using Rawaj.Application.Features.Competitors.AnalyzeCompetitor;
using Rawaj.Application.Features.Competitors.GetCompetitorAnalysis;
using Rawaj.Application.Features.Competitors.GetCompetitors;
using Rawaj.Application.Features.Competitors.ScrapeWebsite;
using Rawaj.Common;

namespace Rawaj.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/competitors")]
public class CompetitorsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(AddCompetitorCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AddCompetitorResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AddCompetitorResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("brand/{brandProfileId:guid}")]
    public async Task<IActionResult> GetByBrand(
        Guid brandProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCompetitorsQuery(brandProfileId, page, pageSize), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<CompetitorSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<PagedResult<CompetitorSummary>>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{competitorId:guid}/analyze")]
    public async Task<IActionResult> Analyze(Guid competitorId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AnalyzeCompetitorCommand(competitorId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<AnalyzeCompetitorResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<AnalyzeCompetitorResponse>.Fail(result.ErrorMessage!));
    }

    [HttpPost("{competitorId:guid}/scrape")]
    public async Task<IActionResult> ScrapeWebsite(Guid competitorId, [FromBody] ScrapeWebsiteRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ScrapeWebsiteCommand(competitorId, request.Url), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<ScrapeWebsiteResponse>.Success(result.Data!))
            : BadRequest(ApiResponse<ScrapeWebsiteResponse>.Fail(result.ErrorMessage!));
    }

    [HttpGet("{competitorId:guid}/analysis")]
    public async Task<IActionResult> GetAnalysis(Guid competitorId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCompetitorAnalysisQuery(competitorId), cancellationToken);

        return result.Succeeded
            ? Ok(ApiResponse<List<RagDocumentSummary>>.Success(result.Data!))
            : BadRequest(ApiResponse<List<RagDocumentSummary>>.Fail(result.ErrorMessage!));
    }

    public record ScrapeWebsiteRequest(string Url);
}
