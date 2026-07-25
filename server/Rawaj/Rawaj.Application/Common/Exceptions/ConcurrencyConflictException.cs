namespace Rawaj.Application.Common.Exceptions;

/// <summary>
/// Raised when a <c>DbUpdateConcurrencyException</c> is caught for a RowVersion-tracked entity —
/// someone else updated the same row between this request's read and its write. Carries a
/// user-facing Arabic message (mirroring CoinPolicy.InsufficientCoinsMessage's convention of
/// keeping the exact wording next to the check that produces it) instead of leaking the raw
/// EF exception to the client.
/// </summary>
public class ConcurrencyConflictException()
    : Exception("تم تعديل هذا العنصر من قِبل مستخدم آخر — يرجى تحديث الصفحة والمحاولة مرة أخرى.");
