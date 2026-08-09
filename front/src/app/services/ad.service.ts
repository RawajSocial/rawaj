import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { Ad, AdStatus } from '../model/ad.model';
import { CampaignPlatform } from '../model/campaign.model';
import { BackendSocialPlatform } from '../model/content-item.model';
import { ScheduledPostSummary } from '../model/scheduled-post.model';
import { CampaignService } from './campaign.service';

const PLATFORM_MAP: Record<BackendSocialPlatform, CampaignPlatform> = {
  Instagram: 'instagram',
  Facebook: 'facebook',
};

const STATUS_MAP: Record<ScheduledPostSummary['status'], AdStatus> = {
  Pending: 'pending',
  Published: 'completed',
  Failed: 'rejected',
  Cancelled: 'paused',
};

/**
 * "My Ads" is backed by ScheduledPost + its PostAnalytics (no dedicated `Ad`
 * entity exists — see feature plan). The scheduled-posts summary doesn't carry
 * post copy/spend, so those UI-only fields default to a stand-in/0 until a
 * richer projection exists.
 */
@Injectable({ providedIn: 'root' })
export class AdService {
  private readonly http = inject(HttpClient);
  private readonly campaignService = inject(CampaignService);
  private readonly baseUrl = `${environment.apiUrl}/scheduled-posts`;

  private readonly _ads = signal<Ad[]>([]);
  readonly ads = this._ads.asReadonly();

  private toAd(s: ScheduledPostSummary): Ad {
    const campaign = s.campaignId ? this.campaignService.getById(s.campaignId)() : undefined;
    const views = s.views ?? 0;
    const clicks = s.clicks ?? 0;
    const name = s.content.trim().substring(0, 40) + (s.content.trim().length > 40 ? '…' : '');
    return {
      id: s.scheduledPostId,
      name: name || `منشور ${s.accountName}`,
      campaignId: s.campaignId ?? '',
      campaignName: campaign?.name ?? 'بدون حملة',
      platforms: [PLATFORM_MAP[s.platform] ?? 'instagram'],
      status: STATUS_MAP[s.status] ?? 'pending',
      format: s.imageUrl ? 'image' : 'text',
      views,
      clicks,
      ctr: views > 0 ? +((clicks / views) * 100).toFixed(2) : 0,
      // No real ad-spend tracking exists yet (no dedicated Ad/spend entity) — stays 0 until one does.
      spend: 0,
      cpc: 0,
      createdAt: s.scheduledAt,
      imageUrl: s.imageUrl ?? undefined,
    };
  }

  getById(id: string) {
    return computed(() => this._ads().find(a => a.id === id));
  }

  refresh(
    brandProfileId: string,
    campaignId?: string | 'all',
    page = 1,
    pageSize = 100,
  ): Observable<ApiResponse<PagedResult<ScheduledPostSummary>>> {
    let url = `${this.baseUrl}?brandProfileId=${encodeURIComponent(brandProfileId)}&page=${page}&pageSize=${pageSize}`;
    if (campaignId && campaignId !== 'all') url += `&campaignId=${encodeURIComponent(campaignId)}`;
    return this.http.get<ApiResponse<PagedResult<ScheduledPostSummary>>>(url).pipe(
      tap(res => {
        if (res.data) this._ads.set(res.data.items.map(s => this.toAd(s)));
      }),
    );
  }

  /** Local-only optimistic toggle — there's no generic pause/resume endpoint for scheduled
   *  posts today (only cancel/publish-now), so this does not persist to the backend. */
  toggle(id: string): void {
    this._ads.update(list =>
      list.map(a => (a.id === id ? { ...a, status: (a.status === 'active' ? 'paused' : 'active') as AdStatus } : a)),
    );
  }
}
