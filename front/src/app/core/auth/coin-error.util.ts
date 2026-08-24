import { HttpErrorResponse } from '@angular/common/http';
import { ApiResponse } from '../../model/auth.model';

// Mirrors CoinPolicy.InsufficientCoinsMessage / CoinPolicy.ScheduledPostCapMessage on the backend —
// change the wording in both places together.
const INSUFFICIENT_COINS_RE = /^You need (\d+) coins? to .+, but only have (\d+)\.$/;
const PLAN_CAP_RE = /^Your subscription plan allows a maximum of (\d+)/;

export interface CoinShortfall {
  required: number;
  balance: number;
}

function messageOf(source: unknown): string | null {
  if (typeof source === 'string') return source;
  if (source instanceof HttpErrorResponse) {
    const body = source.error as ApiResponse<unknown> | undefined;
    return body?.message ?? null;
  }
  return null;
}

/** Accepts an HttpErrorResponse OR a raw per-item error string — the bulk schedule endpoint
 *  returns coin failures inside `Results[].Error` on a 200 OK, which a status-based check alone
 *  would miss. */
export function parseInsufficientCoins(source: unknown): CoinShortfall | null {
  const message = messageOf(source);
  if (!message) return null;
  const match = INSUFFICIENT_COINS_RE.exec(message);
  if (!match) return null;
  return { required: Number(match[1]), balance: Number(match[2]) };
}

export function isPlanCapError(source: unknown): boolean {
  const message = messageOf(source);
  return !!message && PLAN_CAP_RE.test(message);
}
