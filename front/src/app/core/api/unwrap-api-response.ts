import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, map } from 'rxjs';
import { ApiResponse } from '../models';
import { ApiError } from './api-error';

/**
 * Every Rawaj endpoint returns a JSend-style envelope on both success and failure paths (business
 * rule failures come back as 400 with {status:"fail",...} in the body, not just 2xx). This unwraps
 * that envelope into the raw payload on success and normalizes every failure path - envelope
 * "fail"/"error" responses, HTTP error responses still carrying the envelope, and genuine network
 * errors - into a single ApiError so callers only ever handle one error type.
 */
export function unwrapApiResponse<T>() {
  return (source: Observable<ApiResponse<T>>): Observable<T> =>
    source.pipe(
      map((response) => {
        if (response.status === 'success' && response.data !== null) {
          return response.data;
        }
        throw new ApiError(response.message ?? 'Request failed.', response.errors);
      }),
      catchError((error: unknown) => {
        if (error instanceof ApiError) {
          throw error;
        }

        if (error instanceof HttpErrorResponse) {
          const body = error.error as Partial<ApiResponse<T>> | null;
          if (body && typeof body === 'object' && 'status' in body) {
            throw new ApiError(body.message ?? 'Request failed.', body.errors ?? null, error.status);
          }
          throw new ApiError(error.message || 'Network error. Please try again.', null, error.status);
        }

        throw new ApiError('An unexpected error occurred.', null);
      }),
    );
}
