using Rawaj.Domain.Enums;
using Xunit;

namespace Rawaj.Application.Tests.Common;

public class TenantMemberRoleExtensionsTests
{
    [Theory]
    [InlineData(TenantMemberRole.Owner, TenantMemberRole.Viewer, true)]
    [InlineData(TenantMemberRole.Owner, TenantMemberRole.Owner, true)]
    [InlineData(TenantMemberRole.Admin, TenantMemberRole.Owner, false)]
    [InlineData(TenantMemberRole.Editor, TenantMemberRole.Viewer, true)]
    [InlineData(TenantMemberRole.Viewer, TenantMemberRole.Editor, false)]
    public void HasAtLeast_ComparesRoleRank(TenantMemberRole role, TenantMemberRole minimum, bool expected)
    {
        Assert.Equal(expected, role.HasAtLeast(minimum));
    }
}
