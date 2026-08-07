import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import {
  ApproveCampaignPlanResponse, BACKEND_TO_CAMPAIGN_PLATFORM, BACKEND_TO_CAMPAIGN_STATUS,
  Campaign, CampaignDeleteSummary, CampaignPlatform, CampaignSummary,
  CreateCampaignInput, CreateCampaignResponse, DeleteCampaignResult,
  GetCampaignResponse, RefineCampaignPlanResponse,
  ScheduleCampaignPostsResponse, UnarchiveCampaignResponse,
  UpdateCampaignInput,
} from '../model/campaign.model';
import { GenerateCampaignContentInput, GenerateCampaignContentResponse } from '../model/content-item.model';
import { BrandProfileService } from './brand-profile.service';

@Injectable({ providedIn: 'root' })
export class CampaignService {
  private readonly http = inject(HttpClient);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly baseUrl = `${environment.apiUrl}/campaigns`;

  private readonly _campaigns = signal<Campaign[]>([]);
  readonly campaigns = this._campaigns.asReadonly();

  /** Whether the campaign list has ever finished loading in this session. Pages that render off
   *  `campaigns()` need this to tell "no campaigns" apart from "not fetched yet" — without it the
   *  campaigns list flashed its "you have no campaigns" empty state on every cold load, and the
   *  calendar/post pages flashed "campaign not found". Reset by `clear()` on a tenant switch. */
  private readonly _loaded = signal(false);
  readonly loaded = this._loaded.asReadonly();

  private toCampaign(summary: CampaignSummary): Campaign {
    return {
      id: summary.campaignId,
      name: summary.name,
      brandProfileId: summary.brandProfileId,
      status: BACKEND_TO_CAMPAIGN_STATUS[summary.status] ?? 'draft',
      platforms: (summary.targetPlatforms ?? [])
        .map(p => BACKEND_TO_CAMPAIGN_PLATFORM[p])
        .filter((p): p is CampaignPlatform => !!p),
      objective: summary.objective ?? '',
      budget: summary.budgetAmount,
      budgetCurrency: summary.budgetCurrency,
      startDate: summary.startDate ?? '',
      endDate: summary.endDate ?? '',
      createdAt: summary.createdAt,
      planApprovedAt: summary.planApprovedAt,
      onboardingCompletedAt: summary.onboardingCompletedAt,
      contentItemCount: summary.contentItemCount ?? 0,
      adCount: undefined,
      logoUrl: this.brandProfileService.getById(summary.brandProfileId)()?.logoUrl,
    };
  }

  /** Archived campaigns, kept apart from the main list rather than mixed into it. Archiving is
   *  this product's soft delete — the onboarding wizard archives every abandoned draft — so
   *  `GET /campaigns` now excludes them by default (see the backend's `includeArchived`) and they
   *  are fetched on demand, only when the user actually asks to see the archive. Keeping two lists
   *  means the default one's page size isn't eaten by rows the UI would immediately discard. */
  private readonly _archivedCampaigns = signal<Campaign[]>([]);
  readonly archivedCampaigns = this._archivedCampaigns.asReadonly();

  private readonly _archivedLoaded = signal(false);
  readonly archivedLoaded = this._archivedLoaded.asReadonly();

  /** Looks in both lists, so a direct link to an archived campaign still resolves its name and
   *  brand instead of rendering "campaign not found". */
  getById(id: string) {
    return computed(() =>
      this._campaigns().find(c => c.id === id) ?? this._archivedCampaigns().find(c => c.id === id),
    );
  }

  byBrandProfile(brandProfileId: string) {
    return computed(() => this._campaigns().filter(c => c.brandProfileId === brandProfileId));
  }

  /** Fetches non-archived campaigns for a brand (or all accessible brands when omitted). Uses a
   *  large page size since the list backs dropdowns/filters rather than a paginated table. */
  refresh(brandProfileId?: string): Observable<ApiResponse<PagedResult<CampaignSummary>>> {
    let url = `${this.baseUrl}?pageSize=200`;
    if (brandProfileId) url += `&brandProfileId=${encodeURIComponent(brandProfileId)}`;
    return this.http.get<ApiResponse<PagedResult<CampaignSummary>>>(url).pipe(
      tap({
        next: res => {
          if (res.data) this._campaigns.set(res.data.items.map(s => this.toCampaign(s)));
          this._loaded.set(true);
        },
        // A failed fetch still ends the "loading" state — pages must fall through to their own
        // error/empty handling rather than spinning forever.
        error: () => this._loaded.set(true),
      }),
    );
  }

  /** Fetches the archive. The endpoint has no "archived only" mode, so this asks for everything
   *  and keeps the archived rows — the alternative would be a second backend filter for a view
   *  the user opens rarely. */
  refreshArchived(brandProfileId?: string): Observable<ApiResponse<PagedResult<CampaignSummary>>> {
    let url = `${this.baseUrl}?pageSize=200&includeArchived=true`;
    if (brandProfileId) url += `&brandProfileId=${encodeURIComponent(brandProfileId)}`;
    return this.http.get<ApiResponse<PagedResult<CampaignSummary>>>(url).pipe(
      tap({
        next: res => {
          if (res.data) {
            this._archivedCampaigns.set(
              res.data.items.map(s => this.toCampaign(s)).filter(c => c.status === 'archived'),
            );
          }
          this._archivedLoaded.set(true);
        },
        error: () => this._archivedLoaded.set(true),
      }),
    );
  }

  /** Runs `mutation`, then re-fetches the list on success so `campaigns` always reflects server
   *  state. Archive/restore move a campaign between the two lists, so those refresh both. */
  private mutateAndRefresh<T>(
    mutation: Observable<ApiResponse<T>>,
    alsoRefreshArchived = false,
  ): Observable<ApiResponse<T>> {
    return mutation.pipe(
      switchMap(res => {
        if (!res.data) return of(res);
        const refreshes: Observable<unknown>[] = [this.refresh()];
        // Only re-fetch the archive if it's already on screen — otherwise this would fire a
        // second request on every ordinary mutation for a list nobody is looking at.
        if (alsoRefreshArchived && this._archivedLoaded()) refreshes.push(this.refreshArchived());
        return forkJoin(refreshes).pipe(map(() => res));
      }),
    );
  }

  create(input: CreateCampaignInput): Observable<ApiResponse<CreateCampaignResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<CreateCampaignResponse>>(this.baseUrl, input),
    );
  }

  update(campaignId: string, input: UpdateCampaignInput): Observable<ApiResponse<GetCampaignResponse>> {
    // `status` can archive a campaign, so this moves rows between the two lists too.
    return this.mutateAndRefresh(
      this.http.put<ApiResponse<GetCampaignResponse>>(`${this.baseUrl}/${campaignId}`, input),
      true,
    );
  }

  clear(): void {
    this._campaigns.set([]);
    this._loaded.set(false);
    this._archivedCampaigns.set([]);
    this._archivedLoaded.set(false);
  }

  archive(campaignId: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${campaignId}/archive`, {}),
      true,
    );
  }

  /** Restores an archived campaign — back to Active if its plan was already approved, otherwise
   *  Draft (the backend decides; see UnarchiveCampaignCommandHandler). */
  unarchive(campaignId: string): Observable<ApiResponse<UnarchiveCampaignResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<UnarchiveCampaignResponse>>(`${this.baseUrl}/${campaignId}/unarchive`, {}),
      true,
    );
  }

  /** What deleting this campaign will actually do — fetched to populate the delete-confirmation
   *  modal's breakdown before the user commits to it. */
  getDeleteSummary(campaignId: string): Observable<ApiResponse<CampaignDeleteSummary>> {
    return this.http.get<ApiResponse<CampaignDeleteSummary>>(`${this.baseUrl}/${campaignId}/delete-summary`);
  }

  /** Permanently deletes the campaign and cascades to its content items, images and scheduled
   *  posts (pending ones are cancelled on their platform first; already-published posts are left
   *  live — see CampaignDeleteResult). Refreshes both lists since the campaign could have been
   *  deleted from either the default view or the archive. */
  delete(campaignId: string): Observable<ApiResponse<DeleteCampaignResult>> {
    return this.mutateAndRefresh(
      this.http.delete<ApiResponse<DeleteCampaignResult>>(`${this.baseUrl}/${campaignId}`),
      true,
    );
  }

  getCampaign(campaignId: string): Observable<ApiResponse<GetCampaignResponse>> {
    return this.http.get<ApiResponse<GetCampaignResponse>>(`${this.baseUrl}/${campaignId}`);
  }

  /** Free-text "عدّل الخطة" refinement — AI Reasoning Conversation (2,500 coins). */
  refinePlan(campaignId: string, feedback: string): Observable<ApiResponse<RefineCampaignPlanResponse>> {
    return this.http.post<ApiResponse<RefineCampaignPlanResponse>>(
      `${this.baseUrl}/${campaignId}/refine-plan`, { feedback },
    );
  }

  /** Locks in the strategy — gates content generation until this has been called. Refreshes the
   *  list because approval flips the campaign's `planApprovedAt` and `status`, which decide the
   *  next-step CTA on the card and the detail page; without it both kept offering "review the
   *  strategy" for an already-approved campaign until a full reload. */
  approvePlan(campaignId: string): Observable<ApiResponse<ApproveCampaignPlanResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<ApproveCampaignPlanResponse>>(`${this.baseUrl}/${campaignId}/approve-plan`, {}),
    );
  }

  /** Generates a batch of draft posts (+ optional images) for an approved campaign strategy.
   *  Refreshes the list so `contentItemCount` (and therefore the card's stage/CTA) reflects the
   *  posts that were just created. */
  generateContent(campaignId: string, input: GenerateCampaignContentInput): Observable<ApiResponse<GenerateCampaignContentResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<GenerateCampaignContentResponse>>(`${this.baseUrl}/${campaignId}/generate-content`, input),
    );
  }

  /** Bulk-schedules every approved, not-yet-scheduled post in the campaign, routing each item to
   *  the brand's connected account matching that item's own platform. Items with no connected
   *  account for their platform come back `skipped` in the response rather than failing the batch. */
  schedulePosts(campaignId: string): Observable<ApiResponse<ScheduleCampaignPostsResponse>> {
    return this.http.post<ApiResponse<ScheduleCampaignPostsResponse>>(
      `${this.baseUrl}/${campaignId}/schedule-posts`, {},
    );
  }
}
