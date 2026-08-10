using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rawaj.Persistence.Common;

/// <summary>
/// SQL Server's datetime2 has no timezone/offset awareness, and EF Core always returns
/// Kind=Unspecified for it regardless of what was actually stored. Without this, System.Text.Json
/// serializes the value with no 'Z'/offset — a bare "2026-06-04T10:30:00" that's ambiguous to any
/// client, which is how scheduled-post times ended up silently reinterpreted in the wrong timezone
/// downstream. Only apply this to columns that are genuinely always written as true UTC instants (as
/// opposed to a bare calendar date/time with no inherent timezone of its own).
/// </summary>
public static class UtcDateTimeConversionExtensions
{
    public static PropertyBuilder<DateTime> HasUtcConversion(this PropertyBuilder<DateTime> builder)
    {
        builder.HasConversion(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        return builder;
    }

    public static PropertyBuilder<DateTime?> HasUtcConversion(this PropertyBuilder<DateTime?> builder)
    {
        builder.HasConversion(
            v => v.HasValue && v.Value.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        return builder;
    }
}
