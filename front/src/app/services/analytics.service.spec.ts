import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { AnalyticsService } from './analytics.service';
import { PostAnalyticsSnapshot } from '../model/analytics.model';

const SNAPSHOT: PostAnalyticsSnapshot = {
  id: 'snap-1',
  scheduledPostId: 'post-1',
  platform: 'Facebook',
  recordedAt: '2026-01-01T00:00:00Z',
  views: 100,
  uniqueViewers: 80,
  likes: 5,
  comments: 2,
  shares: 1,
  saves: 0,
  clicks: 3,
  engagementRate: 0.08,
};

describe('AnalyticsService', () => {
  let service: AnalyticsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AnalyticsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPost() calls GET /analytics/posts/{id} and returns the full snapshot history', () => {
    let result: PostAnalyticsSnapshot[] | null = null;
    service.getPost('post-1').subscribe(res => (result = res.data));

    const req = httpMock.expectOne(`${environment.apiUrl}/analytics/posts/post-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'success', data: [SNAPSHOT] });

    expect(result).toEqual([SNAPSHOT]);
  });

  it('getPost() surfaces a real fetch failure to the caller instead of swallowing it', () => {
    let errored = false;
    service.getPost('post-1').subscribe({ error: () => (errored = true) });

    const req = httpMock.expectOne(`${environment.apiUrl}/analytics/posts/post-1`);
    req.flush({ status: 'fail', data: null, message: 'Scheduled post not found.', errors: null }, { status: 400, statusText: 'Bad Request' });

    expect(errored).toBe(true);
  });

  it('syncPost() calls POST /analytics/posts/{id}/sync', () => {
    service.syncPost('post-1').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/analytics/posts/post-1/sync`);
    expect(req.request.method).toBe('POST');
    req.flush({ status: 'success', data: SNAPSHOT });
  });
});
