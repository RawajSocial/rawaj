import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { CrmPage } from './crm-page';
import { BrandContextService } from '../../../services/brand-context.service';
import { DashboardOverviewResponse } from '../../../model/dashboard.model';

const OVERVIEW: DashboardOverviewResponse = {
  brandProfileId: 'brand-1',
  campaignId: null,
  postsTracked: 10,
  totalViews: 1000,
  totalUniqueViewers: 500,
  totalLikes: 50,
  totalComments: 10,
  totalShares: 5,
  // Deliberately a precise, non-round fraction: the old client-side reduce()+toFixed(1) would
  // mangle this into a coarse single-decimal value; consuming it directly must not.
  averageEngagementRate: 0.0653,
  platformBreakdown: [
    { platform: 'Instagram', uniqueViewers: 300, views: 600 },
    { platform: 'Facebook', uniqueViewers: 200, views: 400 },
  ],
  topPosts: [],
  bottomPosts: [],
  viewsAvailable: true,
  uniqueViewersAvailable: true,
  engagementRateAvailable: true,
};

describe('CrmPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  /** Creates and initializes CrmPage, flushing the four `refresh()` requests with the given
   *  overview (charts/recent-content/activity are flushed empty — irrelevant to these tests). */
  function createAndLoad(overview: DashboardOverviewResponse): CrmPage {
    const fixture = TestBed.createComponent(CrmPage);
    const brandContext = TestBed.inject(BrandContextService);
    brandContext.setBrandProfile('brand-1');
    fixture.detectChanges();

    httpMock.expectOne(r => r.url.includes('/dashboard/overview')).flush({ status: 'success', data: overview });
    httpMock
      .expectOne(r => r.url.includes('/dashboard/charts'))
      .flush({ status: 'success', data: { brandProfileId: 'brand-1', campaignId: null, points: [] } });
    httpMock.expectOne(r => r.url.includes('/dashboard/recent-content')).flush({ status: 'success', data: [] });
    httpMock.expectOne(r => r.url.includes('/dashboard/activity')).flush({ status: 'success', data: [] });

    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it(
    "activeStats().engagementRate ('all' filter) consumes the backend's weighted " +
      'CampaignEngagementRate directly instead of re-deriving a weighted average client-side (Phase 11)',
    () => {
      const component = createAndLoad(OVERVIEW) as unknown as {
        activeStats: () => { engagementRate: number } | undefined;
      };
      // Exact pass-through of the backend's value — the old reduce()+toFixed(1) path would have
      // rounded this to 0.1, silently losing precision on a value it had no business recomputing.
      expect(component.activeStats()?.engagementRate).toBe(0.0653);
    },
  );

  it(
    "kpis()'s معدل التفاعل card scales the raw fraction into a percentage via formatEngagementRate " +
      '("6.5%", not "0.0653%") instead of appending "%" to the unscaled backend value',
    () => {
      const component = createAndLoad(OVERVIEW);
      const kpis = (component as unknown as { kpis: () => { title: string; value: string }[] }).kpis();
      const engagementKpi = kpis.find(k => k.title === 'معدل التفاعل');
      expect(engagementKpi?.value).toBe('6.5%');
    },
  );
});
