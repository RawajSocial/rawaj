using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Tenants.ArchiveBrandProfile;

public record ArchiveBrandProfileCommand(Guid BrandProfileId) : IRequest<Result<Unit>>;
