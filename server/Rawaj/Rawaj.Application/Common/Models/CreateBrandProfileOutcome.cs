namespace Rawaj.Application.Common.Models;

public enum CreateBrandProfileOutcome
{
    Created,
    AlreadyExisted,
    TenantNotFound,
    LimitReached
}
