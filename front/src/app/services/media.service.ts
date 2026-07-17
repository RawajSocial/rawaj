import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { GeneratedItem } from '../model/generated-item.model';
import { CampaignsApiService } from '../core/api/campaigns-api.service';
import { ContentApiService } from '../core/api/content-api.service';
import { VisualAssetsApiService } from '../core/api/visual-assets-api.service';
import { TenantService } from '../core/tenant/tenant.service';
import { CampaignSummary, ContentRevisionSummary } from '../core/models';

let _nextId = 200;

/**
 * Holds the "generated content" gallery (both AI text content and AI images) for one campaign at
 * a time. Centralized here (rather than per-page) so Content Generation and My Media both react
 * to the same campaign selection without duplicating the load logic.
 */
@Injectable({ providedIn: 'root' })
export class MediaService {
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly contentApi = inject(ContentApiService);
  private readonly visualAssetsApi = inject(VisualAssetsApiService);
  private readonly tenantService = inject(TenantService);

  readonly items = signal<GeneratedItem[]>([]);
  readonly campaigns = signal<CampaignSummary[]>([]);
  readonly selectedCampaignId = signal<string | null>(null);
  readonly loading = signal(false);

  readonly selectedCampaign = computed(() =>
    this.campaigns().find((c) => c.campaignId === this.selectedCampaignId()) ?? null,
  );

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (!brandProfileId) {
        this.campaigns.set([]);
        return;
      }

      this.campaignsApi.getAll(brandProfileId, 1, 50).subscribe({
        next: (result) => {
          this.campaigns.set(result.items);
          if (!this.selectedCampaignId() && result.items.length > 0) {
            this.selectedCampaignId.set(result.items[0].campaignId);
          }
        },
      });
    });

    effect(() => {
      const campaignId = this.selectedCampaignId();
      if (campaignId) {
        this.loadGallery(campaignId);
      } else {
        this.items.set([]);
      }
    });
  }

  private loadGallery(campaignId: string): void {
    this.loading.set(true);

    this.contentApi.getByCampaign(campaignId, 1, 50).subscribe({
      next: (contentResult) => {
        const contentItems: GeneratedItem[] = contentResult.items.map((c) => ({
          id: c.contentItemId,
          type: 'text',
          title: c.content.slice(0, 24) + (c.content.length > 24 ? '…' : ''),
          brand: '',
          status: 'generated',
          createdAt: c.createdAt,
          language: c.language === 'Ar' ? 'ar' : 'en',
          description: c.content,
          textContent: c.content,
          reviewStatus: c.status,
        }));

        this.visualAssetsApi.getByCampaign(campaignId, 1, 50).subscribe({
          next: (assetResult) => {
            const visualItems: GeneratedItem[] = assetResult.items.map((v) => ({
              id: v.visualAssetId,
              type: 'static-ad',
              title: v.type,
              brand: '',
              status: 'generated',
              createdAt: v.createdAt,
              thumbnailUrl: v.fileUrl,
              isApproved: v.isApproved,
            }));
            this.items.set(
              [...contentItems, ...visualItems].sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
            );
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  nextId(): string {
    return String(++_nextId);
  }

  add(item: GeneratedItem): void {
    this.items.update(list => [item, ...list]);
  }

  markGenerated(id: string, extra?: Partial<GeneratedItem>): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, status: 'generated', ...extra } : i)
    );
  }

  markFailed(id: string): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, status: 'failed' } : i)
    );
  }

  update(updated: GeneratedItem): void {
    this.items.update(list => list.map(i => i.id === updated.id ? updated : i));
  }

  remove(id: string): void {
    this.items.update(list => list.filter(i => i.id !== id));
  }

  /** Approves or rejects a text content item. Only Approved content is eligible for scheduling
   * (server-enforced). */
  reviewContent(contentItemId: string, approve: boolean): Observable<{ contentItemId: string; status: string; reviewedAt: string }> {
    return this.contentApi.review(contentItemId, approve).pipe(
      tap((result) => {
        this.items.update((list) =>
          list.map((i) => (i.id === contentItemId ? { ...i, reviewStatus: result.status as GeneratedItem['reviewStatus'] } : i)),
        );
      }),
    );
  }

  regenerate(contentItemId: string, feedback: string) {
    return this.contentApi.regenerate(contentItemId, feedback).pipe(
      tap((result) => {
        this.items.update((list) =>
          list.map((i) =>
            i.id === contentItemId
              ? { ...i, textContent: result.content, description: result.content, reviewStatus: result.status }
              : i,
          ),
        );
      }),
    );
  }

  getRevisions(contentItemId: string): Observable<ContentRevisionSummary[]> {
    return this.contentApi.getRevisions(contentItemId);
  }

  reviewVisualAsset(visualAssetId: string, approve: boolean) {
    return this.visualAssetsApi.review(visualAssetId, approve).pipe(
      tap((result) => {
        this.items.update((list) =>
          list.map((i) => (i.id === visualAssetId ? { ...i, isApproved: result.isApproved } : i)),
        );
      }),
    );
  }
}
