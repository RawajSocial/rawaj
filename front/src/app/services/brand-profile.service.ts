import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import {
  BrandProfile, BrandProfileDetail, BrandProfileSummary, CreateBrandProfileResponse, UpdateBrandProfileRequest,
  UpdateBrandProfileResponse,
} from '../model/brand-profile.model';
import { resolveMediaUrl } from '../core/auth/media-url.util';

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
    logoUrl: resolveMediaUrl(summary.logoUrl),
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

  /** Full record for one brand (unlike `profiles`, includes targetAudience/websiteUrl/languages/keywords) —
   *  not cached, always fetched fresh, since the detail page is the only consumer. */
  getDetail(id: string): Observable<ApiResponse<BrandProfileDetail>> {
    return this.http.get<ApiResponse<BrandProfileDetail>>(`${this.baseUrl}/${id}`).pipe(
      tap(res => {
        if (res.data?.logoUrl) res.data.logoUrl = resolveMediaUrl(res.data.logoUrl);
      }),
    );
  }

  update(id: string, request: UpdateBrandProfileRequest): Observable<ApiResponse<UpdateBrandProfileResponse>> {
    return this.mutateAndRefresh(
      this.http.put<ApiResponse<UpdateBrandProfileResponse>>(`${this.baseUrl}/${id}`, request),
    );
  }

  /** Uploads a logo file and resolves to the stored `/media/...` path (in the response body). */
  uploadLogo(file: File): Observable<ApiResponse<{ logoUrl: string }>> {
    const formData = new FormData();
    formData.append('logo', file);
    return this.http.post<ApiResponse<{ logoUrl: string }>>(`${this.baseUrl}/logo`, formData);
  }

  archive(id: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/archive`, {}),
    );
  }

  /** Real AI follow-up questions for the onboarding wizard's step-7 chat (3–8 questions, each with
   *  3 quick-reply suggestions) — grounded in whatever the wizard has collected so far. Charges
   *  AI Reasoning Conversation coins. `onboardingContext` is passed through as raw JSON. */
  generateOnboardingQuestions(
    brandProfileId: string, onboardingContext: unknown,
  ): Observable<ApiResponse<{ questionsJson: string }>> {
    return this.http.post<ApiResponse<{ questionsJson: string }>>(
      `${this.baseUrl}/${brandProfileId}/onboarding-questions`, { onboardingContext },
    );
  }
}
