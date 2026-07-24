/**
 * GET /scheduled-posts/posting-time-suggestions returns a (platform, dayOfWeek, hour) pair, not a
 * concrete datetime — this computes the next real occurrence of that weekday/hour in LOCAL time,
 * pushed out to the following week if today's slot has already passed (or is too close to "now"
 * to satisfy the backend's 10-minute scheduling floor).
 */
export function nextOccurrence(dayOfWeek: number, hour: number, now = new Date(), minLeadMinutes = 15): Date {
  const candidate = new Date(now);
  candidate.setHours(hour, 0, 0, 0);
  const dayDiff = (dayOfWeek - now.getDay() + 7) % 7;
  candidate.setDate(now.getDate() + dayDiff);

  const minAllowed = new Date(now.getTime() + minLeadMinutes * 60_000);
  if (candidate < minAllowed) {
    candidate.setDate(candidate.getDate() + 7);
  }
  return candidate;
}

/** `YYYY-MM-DD`, for a `<input type="date">` value, in local time (not UTC). */
export function toDateInputValue(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** `HH:mm`, for a `<input type="time">` value, in local time. */
export function toTimeInputValue(d: Date): string {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
