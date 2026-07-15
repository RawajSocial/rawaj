namespace Rawaj.Domain.Enums;

public static class TenantMemberRoleExtensions
{
    private static readonly Dictionary<TenantMemberRole, int> Ranks = new()
    {
        [TenantMemberRole.Viewer] = 0,
        [TenantMemberRole.Editor] = 1,
        [TenantMemberRole.Admin] = 2,
        [TenantMemberRole.Owner] = 3
    };

    public static bool HasAtLeast(this TenantMemberRole role, TenantMemberRole minimumRole) =>
        Ranks[role] >= Ranks[minimumRole];
}
