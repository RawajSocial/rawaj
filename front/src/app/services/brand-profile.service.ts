import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { BrandProfile, BrandProfileSummary, CreateBrandProfileResponse } from '../model/brand-profile.model';

export interface CreateBrandProfileInput {
  name: string;
  description?: string;
  brandVoice?: BrandProfile['brandVoice'];
  tagline?: string;
  industry?: string;
  targetAudience?: string;
  colors?: string[];
  logoUrl?: string;
  websiteUrl?: string;
  supportedLanguages?: string[];
  keywords?: string[];
  location?: string;
}

function toBrandProfile(summary: BrandProfileSummary): BrandProfile {
  return {
    id: summary.brandProfileId,
    name: summary.name,
    description: summary.description,
    brandVoice: summary.brandVoice,
    status: summary.status,
    isDefault: summary.isDefault,
    tagline: summary.tagline,
    industry: summary.industry,
    colors: summary.colors,
    logoUrl: summary.logoUrl,
    supportedLanguages: [],
    keywords: [],
  };
}

@Injectable({ providedIn: 'root' })
export class BrandProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/brand-profiles`;

  private readonly _profiles = signal<BrandProfile[]>([]);
  readonly profiles = this._profiles.asReadonly();

  getById(id: string) {
    return computed(() => this._profiles().find(p => p.id === id));
  }

  refresh(): Observable<ApiResponse<BrandProfileSummary[]>> {
    return this.http.get<ApiResponse<BrandProfileSummary[]>>(this.baseUrl).pipe(
      tap(res => {
        if (res.data) this._profiles.set(res.data.map(toBrandProfile));
      }),
    );
  }

  /** Runs `mutation`, then re-fetches the list on success so `profiles` always reflects server state. */
  private mutateAndRefresh<T>(mutation: Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    return mutation.pipe(
      switchMap(res => (res.data ? this.refresh().pipe(map(() => res)) : of(res))),
    );
  }

  create(input: CreateBrandProfileInput): Observable<ApiResponse<CreateBrandProfileResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<CreateBrandProfileResponse>>(this.baseUrl, input),
    );
  }

  archive(id: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/archive`, {}),
    );
  }
}
