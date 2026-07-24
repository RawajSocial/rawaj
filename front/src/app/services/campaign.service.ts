import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import {
  ApproveCampaignPlanResponse,
  BackendCampaignStatus, Campaign, CampaignStatus, CampaignSummary,
  CreateCampaignInput, CreateCampaignResponse, GenerateBusinessDiagnosisResponse,
  GenerateMarketingPlanResponse, GetCampaignResponse, RefineCampaignPlanResponse,
  ResearchCampaignCompetitorsResponse, ScheduleCampaignPostsResponse, UpdateCampaignInput,
} from '../model/campaign.model';
import { GenerateCampaignContentInput, GenerateCampaignContentResponse } from '../model/content-item.model';
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

  /** Best-effort Tavily competitor research — charges coins only when it actually finds data;
   *  never throws a "flow failure" the caller needs to special-case (see backend doc comment). */
  researchCompetitors(campaignId: string): Observable<ApiResponse<ResearchCampaignCompetitorsResponse>> {
    return this.http.post<ApiResponse<ResearchCampaignCompetitorsResponse>>(
      `${this.baseUrl}/${campaignId}/research-competitors`, {},
    );
  }

  /** "What we understood about your business" — AI Business Diagnosis (4,000 coins). */
  diagnoseBusiness(campaignId: string): Observable<ApiResponse<GenerateBusinessDiagnosisResponse>> {
    return this.http.post<ApiResponse<GenerateBusinessDiagnosisResponse>>(
      `${this.baseUrl}/${campaignId}/diagnose-business`, {},
    );
  }

  /** Complete Marketing Strategy — free on the tenant's first campaign, 12,000 coins after. */
  generatePlan(campaignId: string): Observable<ApiResponse<GenerateMarketingPlanResponse>> {
    return this.http.post<ApiResponse<GenerateMarketingPlanResponse>>(
      `${this.baseUrl}/${campaignId}/generate-plan`, {},
    );
  }

  /** Free-text "عدّل الخطة" refinement — AI Reasoning Conversation (2,500 coins). */
  refinePlan(campaignId: string, feedback: string): Observable<ApiResponse<RefineCampaignPlanResponse>> {
    return this.http.post<ApiResponse<RefineCampaignPlanResponse>>(
      `${this.baseUrl}/${campaignId}/refine-plan`, { feedback },
    );
  }

  /** Locks in the strategy — gates content generation until this has been called. */
  approvePlan(campaignId: string): Observable<ApiResponse<ApproveCampaignPlanResponse>> {
    return this.http.post<ApiResponse<ApproveCampaignPlanResponse>>(
      `${this.baseUrl}/${campaignId}/approve-plan`, {},
    );
  }

  /** Generates a batch of draft posts (+ optional images) for an approved campaign strategy. */
  generateContent(campaignId: string, input: GenerateCampaignContentInput): Observable<ApiResponse<GenerateCampaignContentResponse>> {
    return this.http.post<ApiResponse<GenerateCampaignContentResponse>>(
      `${this.baseUrl}/${campaignId}/generate-content`, input,
    );
  }

  /** Bulk-schedules every approved, not-yet-scheduled post in the campaign to one social account. */
  schedulePosts(campaignId: string, socialAccountId: string): Observable<ApiResponse<ScheduleCampaignPostsResponse>> {
    return this.http.post<ApiResponse<ScheduleCampaignPostsResponse>>(
      `${this.baseUrl}/${campaignId}/schedule-posts`, { socialAccountId },
    );
  }
}
