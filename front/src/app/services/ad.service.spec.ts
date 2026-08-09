import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { AdService } from './ad.service';
import { ScheduledPostSummary } from '../model/scheduled-post.model';
import { PagedResult } from '../model/paged-result.model';

const SUMMARY: ScheduledPostSummary = {
  scheduledPostId: 'post-1',
  contentItemId: 'content-1',
  brandProfileId: 'brand-1',
  campaignId: null,
  platform: 'Instagram',
  accountName: 'Test Page',
  scheduledAt: '2026-01-01T00:00:00Z',
  status: 'Published',
  publishedAt: '2026-01-01T00:00:00Z',
  errorMessage: null,
  views: 200,
  uniqueViewers: 150,
  likes: 10,
  comments: 2,
  shares: 1,
  clicks: 20,
  engagementRate: 0.065,
  content: 'Hello world',
  imageUrl: null,
};

describe('AdService', () => {
  let service: AdService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('maps ScheduledPostSummary.views (not a fabricated "impressions" concept) onto Ad.views', () => {
    service.refresh('brand-1').subscribe();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/scheduled-posts?brandProfileId=brand-1&page=1&pageSize=100`,
    );
    const page: PagedResult<ScheduledPostSummary> = { items: [SUMMARY], totalCount: 1, page: 1, pageSize: 100 };
    req.flush({ status: 'success', data: page });

    const ad = service.getById('post-1')();
    expect(ad).toBeTruthy();
    expect(ad!.views).toBe(200);
    expect(ad!.ctr).toBe(10); // 20 clicks / 200 views * 100
  });

  it('ctr is 0 when views is 0, instead of dividing by zero', () => {
    service.refresh('brand-1').subscribe();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/scheduled-posts?brandProfileId=brand-1&page=1&pageSize=100`,
    );
    const page: PagedResult<ScheduledPostSummary> = {
      items: [{ ...SUMMARY, views: null, clicks: 5 }],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    };
    req.flush({ status: 'success', data: page });

    const ad = service.getById('post-1')();
    expect(ad!.views).toBe(0);
    expect(ad!.ctr).toBe(0);
  });
});
