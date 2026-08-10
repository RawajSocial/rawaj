/**
 * Formatting shared by every campaign surface (list card, detail page, calendar, post detail).
 *
 * The campaign pages each used to inline their own version of this — or, more often, print the
 * backend's raw values straight into the template: `{{ campaign().startDate }} — {{ endDate }}`
 * rendered an ISO date and a lone "—" separator when a campaign had no dates set, and the detail
 * page's budget was an unformatted number glued to a currency code.
 */

const DATE_LOCALE = 'ar-EG';

/** A single `DateOnly`/ISO date from the API as a short readable date. Returns `null` — not a
 *  fabricated placeholder — when the value is missing or unparseable, so callers decide what an
 *  absent date should look like in their own layout. */
export function formatCampaignDate(value: string | null | undefined): string | null {
  if (!value) return null;
  const parsed = new Date(value);
  if (isNaN(parsed.getTime())) return null;
  return parsed.toLocaleDateString(DATE_LOCALE, { day: 'numeric', month: 'short', year: 'numeric' });
}

/** The campaign period. A half-set range renders the end it does have ("من 1 يناير" / "حتى 1 مارس")
 *  rather than pairing a real date with an empty string. */
export function formatCampaignDateRange(
  start: string | null | undefined,
  end: string | null | undefined,
): string {
  const from = formatCampaignDate(start);
  const to = formatCampaignDate(end);
  if (from && to) return `${from} — ${to}`;
  if (from) return `من ${from}`;
  if (to) return `حتى ${to}`;
  return 'لم تُحدَّد';
}

/** Budget + currency, thousands-separated. `null`/0 is a genuinely unset budget, not "0 EGP". */
export function formatCampaignBudget(
  amount: number | null | undefined,
  currency: string | null | undefined,
): string {
  if (amount === null || amount === undefined) return '—';
  return `${amount.toLocaleString(DATE_LOCALE)} ${currency ?? ''}`.trim();
}
