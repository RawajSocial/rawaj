using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.GetMyMemberships;

/// <summary>
/// Lists every tenant the current user belongs to (their own auto-provisioned tenant plus any
/// tenant they've accepted an invite into) — deliberately does NOT implement
/// <c>IRequireTenantRole</c>, since resolving "the" active tenant is exactly what this endpoint
/// helps the frontend avoid needing to guess.
/// </summary>
public record GetMyMembershipsQuery : IRequest<Result<List<MembershipSummary>>>;
