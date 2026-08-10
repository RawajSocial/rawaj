/**
 * Rawaj's tenants are all Cairo-based today. Scheduled-post times are stored and transmitted as
 * true UTC instants (ISO strings with a 'Z'), but every human-facing date/time INPUT or DISPLAY —
 * the calendar, the reschedule modal, the "schedule for" pickers — must be interpreted/rendered in
 * Cairo local time regardless of the viewing browser's own timezone. Mixing those two up (treating a
 * date/time-input value as if it were already UTC, or displaying a UTC instant with the browser's
 * ambient timezone) is what caused scheduled posts to publish ~2 hours off from what was intended.
 */
const CAIRO_TIME_ZONE = 'Africa/Cairo';

/** Cairo's UTC offset (in minutes) at the given instant, computed via Intl rather than a hardcoded
 *  +2 so this stays correct if Egypt's DST rules ever change again (as they briefly did in 2023). */
function cairoOffsetMinutes(instant: Date): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: CAIRO_TIME_ZONE,
    hour12: false,
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  }).formatToParts(instant).reduce<Record<string, string>>((acc, p) => {
    acc[p.type] = p.value;
    return acc;
  }, {});

  // Intl renders midnight as "24" for hour12: false in some engines — normalize back to 0.
  const hour = parts['hour'] === '24' ? 0 : Number(parts['hour']);
  const asUtc = Date.UTC(
    Number(parts['year']), Number(parts['month']) - 1, Number(parts['day']),
    hour, Number(parts['minute']), Number(parts['second']),
  );
  return (asUtc - instant.getTime()) / 60000;
}

/** Converts a "YYYY-MM-DD" + "HH:mm" pair — as typed into a plain date/time input, meaning Cairo
 *  wall-clock time — into a true UTC ISO string ready to send to the backend. */
export function cairoLocalToUtcIso(dateStr: string, timeStr: string): string {
  // First guess the offset using the naive value treated as if it were UTC (accurate except in the
  // rare case of a DST-transition hour, which Egypt does not currently observe).
  const naiveAsUtc = new Date(`${dateStr}T${timeStr}:00Z`);
  const offsetMinutes = cairoOffsetMinutes(naiveAsUtc);
  return new Date(naiveAsUtc.getTime() - offsetMinutes * 60000).toISOString();
}

/** Converts a stored UTC instant (ISO string, with or without 'Z') into the "YYYY-MM-DD"/"HH:mm"
 *  parts a date/time input should show, in Cairo local time. */
export function utcIsoToCairoLocalParts(iso: string): { date: string; time: string } {
  const instant = new Date(hasOffset(iso) ? iso : `${iso}Z`);
  const date = new Intl.DateTimeFormat('en-CA', {
    timeZone: CAIRO_TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit',
  }).format(instant);
  const time = new Intl.DateTimeFormat('en-GB', {
    timeZone: CAIRO_TIME_ZONE, hour: '2-digit', minute: '2-digit', hour12: false,
  }).format(instant);
  return { date, time };
}

/** Cairo calendar-date key ("YYYY-MM-DD") for grouping posts by day — NOT the same as slicing the
 *  first 10 characters of a UTC ISO string, which gives the UTC calendar date and can be off by one
 *  day for the ~2-hour window where Cairo has already crossed into the next day but UTC hasn't. */
export function cairoDateKey(iso: string): string {
  return utcIsoToCairoLocalParts(iso).date;
}

/** Formats a stored UTC instant for display, always in Cairo time regardless of the viewer's own
 *  device/browser timezone. */
export function formatCairoTime(iso: string, options: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit', hour12: true }): string {
  const instant = new Date(hasOffset(iso) ? iso : `${iso}Z`);
  return new Intl.DateTimeFormat('ar-EG', { ...options, timeZone: CAIRO_TIME_ZONE }).format(instant);
}

export function formatCairoDate(iso: string, options: Intl.DateTimeFormatOptions): string {
  const instant = new Date(hasOffset(iso) ? iso : `${iso}Z`);
  return new Intl.DateTimeFormat('ar-EG', { ...options, timeZone: CAIRO_TIME_ZONE }).format(instant);
}

function hasOffset(iso: string): boolean {
  return iso.endsWith('Z') || /[+-]\d\d:\d\d$/.test(iso);
}
