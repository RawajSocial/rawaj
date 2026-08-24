import { cairoLocalToUtcIso, utcIsoToCairoLocalParts } from './cairo-time.util';

/**
 * GET /scheduled-posts/posting-time-suggestions returns a (platform, dayOfWeek, hour) pair, not a
 * concrete datetime — this computes the next real occurrence of that weekday/hour in CAIRO local
 * time (the suggestion itself is Cairo-local — see the backend's PostingTimeIntelligence), pushed
 * out to the following week if today's slot has already passed (or is too close to "now" to satisfy
 * the backend's 10-minute scheduling floor). Returns date/time INPUT VALUES directly rather than a
 * JS Date, since a Date's own getters/setters are the viewer's browser-local timezone and would
 * reintroduce the exact mismatch this exists to avoid.
 */
export function nextCairoOccurrence(
  dayOfWeek: number, hour: number, nowUtc = new Date(), minLeadMinutes = 15,
): { date: string; time: string } {
  const todayCairoDate = utcIsoToCairoLocalParts(nowUtc.toISOString()).date;
  const todayCairoDow = new Date(`${todayCairoDate}T00:00:00Z`).getUTCDay();

  let candidateDate = addDaysToDateStr(todayCairoDate, (dayOfWeek - todayCairoDow + 7) % 7);
  const hourStr = `${String(hour).padStart(2, '0')}:00`;
  let candidateIso = cairoLocalToUtcIso(candidateDate, hourStr);

  const minAllowed = nowUtc.getTime() + minLeadMinutes * 60_000;
  if (new Date(candidateIso).getTime() < minAllowed) {
    candidateDate = addDaysToDateStr(candidateDate, 7);
    candidateIso = cairoLocalToUtcIso(candidateDate, hourStr);
  }
  return utcIsoToCairoLocalParts(candidateIso);
}

/** Today's day-of-week (0 = Sunday) in Cairo's calendar, not the browser's — for callers that need
 *  a "same day" fallback when no platform-specific suggestion is available. */
export function cairoTodayDayOfWeek(nowUtc = new Date()): number {
  const todayCairoDate = utcIsoToCairoLocalParts(nowUtc.toISOString()).date;
  return new Date(`${todayCairoDate}T00:00:00Z`).getUTCDay();
}

function addDaysToDateStr(dateStr: string, days: number): string {
  const d = new Date(`${dateStr}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}
