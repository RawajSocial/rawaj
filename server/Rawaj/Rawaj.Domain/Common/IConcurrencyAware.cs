namespace Rawaj.Domain.Common;

/// <summary>
/// Marks an entity as RowVersion-tracked for optimistic concurrency. SQL Server's `rowversion`
/// column type auto-generates this on every write and ignores whatever the app sets — but the
/// EF Core InMemory provider (used by the test suite) has no equivalent auto-generation, so
/// AppDbContext.SaveChangesAsync manually bumps this for every Added/Modified entry of this type,
/// which is a harmless no-op against a real SQL Server column but is what makes
/// DbUpdateConcurrencyException actually reproducible in tests.
/// </summary>
public interface IConcurrencyAware
{
    byte[] RowVersion { get; set; }
}
