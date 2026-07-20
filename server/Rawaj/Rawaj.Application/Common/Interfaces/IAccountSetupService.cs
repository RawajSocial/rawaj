using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IAccountSetupService
{
    Task<GetAccountSetupResult> GetAsync(Guid userId, Guid brandProfileId, CancellationToken cancellationToken);

    Task<SaveAccountSetupResult> SaveAsync(Guid userId, Guid brandProfileId, AccountSetupFields fields, CancellationToken cancellationToken);
}
