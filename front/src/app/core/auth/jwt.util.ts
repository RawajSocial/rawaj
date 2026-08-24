import { AccessTokenClaims } from '../../model/auth.model';

/** Decodes a JWT's payload without verifying its signature (verification is the backend's job). */
export function decodeAccessToken(token: string): AccessTokenClaims | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const json = decodeURIComponent(
      atob(padded)
        .split('')
        .map(c => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(json) as AccessTokenClaims;
  } catch {
    return null;
  }
}

/** True once the token's `exp` (seconds since epoch) is in the past. */
export function isTokenExpired(claims: AccessTokenClaims): boolean {
  return Date.now() >= claims.exp * 1000;
}
