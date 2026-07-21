using Rawaj.Application.Common.Models;
using Xunit;

namespace Rawaj.Application.Tests.Common;

public class PaginationDefaultsTests
{
    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(3, 0, 3, PaginationDefaults.DefaultPageSize)]
    [InlineData(3, -10, 3, PaginationDefaults.DefaultPageSize)]
    [InlineData(2, 500, 2, PaginationDefaults.MaxPageSize)]
    [InlineData(5, 30, 5, 30)]
    public void Clamp_KeepsPageAndPageSizeWithinBounds(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var (clampedPage, clampedPageSize) = PaginationDefaults.Clamp(page, pageSize);

        Assert.Equal(expectedPage, clampedPage);
        Assert.Equal(expectedPageSize, clampedPageSize);
    }

    [Fact]
    public void PagedResult_ComputesTotalPages()
    {
        var result = new PagedResult<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 25);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void PagedResult_WithZeroPageSize_ReturnsZeroTotalPages()
    {
        var result = new PagedResult<int>([], Page: 1, PageSize: 0, TotalCount: 25);

        Assert.Equal(0, result.TotalPages);
    }
}
