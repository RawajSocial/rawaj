import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import {
  BackendCampaignStatus, Campaign, CampaignStatus, CampaignSummary,
  CreateCampaignInput, CreateCampaignResponse, GetCampaignResponse, UpdateCampaignInput,
} from '../model/campaign.model';
import { BrandProfileService } from './brand-profile.service';

const STATUS_MAP: Record<BackendCampaignStatus, CampaignStatus> = {
  Draft: 'draft',
  Active: 'active',
  Paused: 'paused',
  Completed: 'completed',
  Archived: 'archived',
};

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly http = inject(HttpClient);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly baseUrl = `${environment.apiUrl}/campaigns`;

  private readonly _campaigns = signal<Campaign[]>([]);
  readonly campaigns = this._campaigns.asReadonly();

  private toCampaign(summary: CampaignSummary): Campaign {
    return {
      id: summary.campaignId,
      name: summary.name,
      brandProfileId: summary.brandProfileId,
      status: STATUS_MAP[summary.status] ?? 'draft',
      platforms: [],
      objective: 'awareness',
      budget: 0,
      spent: 0,
      reach: 0,
      clicks: 0,
      ctr: 0,
      startDate: summary.startDate ?? '',
      endDate: summary.endDate ?? '',
      createdAt: summary.createdAt,
      adCount: undefined,
      logoUrl: this.brandProfileService.getById(summary.brandProfileId)()?.logoUrl,
    };
  }

  getById(id: string) {
    return computed(() => this._campaigns().find(c => c.id === id));
  }

  byBrandProfile(brandProfileId: string) {
    return computed(() => this._campaigns().filter(c => c.brandProfileId === brandProfileId));
  }

  /** Fetches campaigns for a brand (or all accessible brands when omitted). Uses a large
   *  page size since the list backs dropdowns/filters rather than a paginated table. */
  refresh(brandProfileId?: string): Observable<ApiResponse<PagedResult<CampaignSummary>>> {
    let url = `${this.baseUrl}?pageSize=200`;
    if (brandProfileId) url += `&brandProfileId=${encodeURIComponent(brandProfileId)}`;
    return this.http.get<ApiResponse<PagedResult<CampaignSummary>>>(url).pipe(
      tap(res => {
        if (res.data) this._campaigns.set(res.data.items.map(s => this.toCampaign(s)));
      }),
    );
  }

  /** Runs `mutation`, then re-fetches the list on success so `campaigns` always reflects server state. */
  private mutateAndRefresh<T>(mutation: Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    return mutation.pipe(
      switchMap(res => (res.data ? this.refresh().pipe(map(() => res)) : of(res))),
    );
  }

  create(input: CreateCampaignInput): Observable<ApiResponse<CreateCampaignResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<CreateCampaignResponse>>(this.baseUrl, input),
    );
  }

  update(campaignId: string, input: UpdateCampaignInput): Observable<ApiResponse<GetCampaignResponse>> {
    return this.mutateAndRefresh(
      this.http.put<ApiResponse<GetCampaignResponse>>(`${this.baseUrl}/${campaignId}`, input),
    );
  }

  archive(campaignId: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${campaignId}/archive`, {}),
    );
  }

  getCampaign(campaignId: string): Observable<ApiResponse<GetCampaignResponse>> {
    return this.http.get<ApiResponse<GetCampaignResponse>>(`${this.baseUrl}/${campaignId}`);
  }
}
