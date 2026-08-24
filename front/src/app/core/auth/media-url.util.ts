import { environment } from '../../../environments/environment';

const API_ORIGIN = new URL(environment.apiUrl).origin;

/**
 * Backend avatar/media URLs are root-relative (`/media/...`); resolve them against the API's
 * origin, since the SPA and API are served from different origins in dev.
 */
export function resolveMediaUrl(url: string | null | undefined): string | undefined {
  if (!url) return undefined;
  return url.startsWith('/') ? `${API_ORIGIN}${url}` : url;
}
