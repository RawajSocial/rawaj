import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { BrandContextService } from './brand-context.service';
import { BrandProfileService } from './brand-profile.service';
import { BrandProfileSummary } from '../model/brand-profile.model';

const BRAND_KEY = 'rawaj.brandContext.brandProfileId';
const CAMPAIGN_KEY = 'rawaj.brandContext.campaignId';

const PROFILE_A: BrandProfileSummary = {
  brandProfileId: 'brand-a', name: 'A', status: 'active', isDefault: false, colors: [],
};
const PROFILE_B: BrandProfileSummary = {
  brandProfileId: 'brand-b', name: 'B', status: 'active', isDefault: false, colors: [],
};

describe('BrandContextService', () => {
  let service: BrandContextService;
  let brandProfileService: BrandProfileService;
  let httpMock: HttpTestingController;

  afterEach(() => {
    localStorage.removeItem(BRAND_KEY);
    localStorage.removeItem(CAMPAIGN_KEY);
  });

  function setup(): void {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(BrandContextService);
    brandProfileService = TestBed.inject(BrandProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  }

  /** Drives BrandProfileService.refresh() through the testing HTTP backend, then flushes the
   *  service's own effects so the validation effect (keyed off `profiles()`) has a chance to run. */
  function loadProfiles(profiles: BrandProfileSummary[]): void {
    brandProfileService.refresh().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/brand-profiles`).flush({ status: 'success', data: profiles });
    TestBed.tick();
  }

  it('clear() resets selection state and removes persisted storage', () => {
    localStorage.setItem(BRAND_KEY, 'brand-a');
    localStorage.setItem(CAMPAIGN_KEY, 'camp-1');
    setup();

    service.clear();

    expect(service.selectedBrandProfileId()).toBeNull();
    expect(service.selectedCampaignId()).toBe('all');
    expect(localStorage.getItem(BRAND_KEY)).toBeNull();
    expect(localStorage.getItem(CAMPAIGN_KEY)).toBeNull();
  });

  it('clears a selection missing from the freshly-loaded (tenant-scoped) profile list and falls back to a real default', () => {
    // Simulates a brand id left over from a different tenant/login on the same browser.
    localStorage.setItem(BRAND_KEY, 'stale-brand-from-another-tenant');
    setup();

    loadProfiles([PROFILE_A, PROFILE_B]);

    expect(service.selectedBrandProfileId()).toBe('brand-a');
    expect(localStorage.getItem(BRAND_KEY)).toBe('brand-a');
  });

  it('leaves a selection untouched when it belongs to the loaded profile list', () => {
    localStorage.setItem(BRAND_KEY, 'brand-b');
    setup();

    loadProfiles([PROFILE_A, PROFILE_B]);

    expect(service.selectedBrandProfileId()).toBe('brand-b');
    expect(localStorage.getItem(BRAND_KEY)).toBe('brand-b');
  });
});
