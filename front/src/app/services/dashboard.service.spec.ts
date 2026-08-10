import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { DashboardService } from './dashboard.service';
import { DashboardChartsResponse } from '../model/dashboard.model';

const CHARTS_RESPONSE: DashboardChartsResponse = {
  brandProfileId: 'brand-1',
  campaignId: 'campaign-1',
  points: [
    { date: '2026-01-01', uniqueViewers: 40, views: 100, likes: 5, comments: 1, shares: 0, engagementRate: 0.06 },
  ],
};

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getCharts() scopes the request to a campaign and returns Views/UniqueViewers-named points', () => {
    let result: DashboardChartsResponse | null = null;
    service.getCharts('brand-1', 'campaign-1', 30).subscribe(res => (result = res.data));

    const req = httpMock.expectOne(
      `${environment.apiUrl}/dashboard/charts?brandProfileId=brand-1&campaignId=campaign-1&days=30`,
    );
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'success', data: CHARTS_RESPONSE });

    expect(result).toEqual(CHARTS_RESPONSE);
    expect(result!.points[0].uniqueViewers).toBe(40);
    expect(result!.points[0].views).toBe(100);
  });

  it('getCharts() omits campaignId when scoped to the whole brand ("all")', () => {
    service.getCharts('brand-1', 'all', 14).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/dashboard/charts?brandProfileId=brand-1&days=14`);
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'success', data: CHARTS_RESPONSE });
  });

  it('getCharts() does not touch the shared charts() signal populated by refresh()', () => {
    service.getCharts('brand-1', 'campaign-1', 30).subscribe();

    const req = httpMock.expectOne(
      `${environment.apiUrl}/dashboard/charts?brandProfileId=brand-1&campaignId=campaign-1&days=30`,
    );
    req.flush({ status: 'success', data: CHARTS_RESPONSE });

    expect(service.charts()).toBeNull();
  });
});
