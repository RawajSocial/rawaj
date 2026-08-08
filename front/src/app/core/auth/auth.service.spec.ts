import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { vi } from 'vitest';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { TenantService } from '../tenant/tenant.service';
import { BrandContextService } from '../../services/brand-context.service';

describe('AuthService', () => {
  let service: AuthService;
  let tenantService: TenantService;
  let brandContextService: BrandContextService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    tenantService = TestBed.inject(TenantService);
    brandContextService = TestBed.inject(BrandContextService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // clearSession() is the single choke point behind logout(), the 401 interceptor's unrecoverable
  // case, and app-bootstrap failure - a brand/tenant id that survives it leaks into whatever
  // session starts next on the same browser (the exact bug this covers).
  it('clearSession() clears tenant and brand context state alongside the tokens', () => {
    const tenantClear = vi.spyOn(tenantService, 'clear');
    const brandClear = vi.spyOn(brandContextService, 'clear');

    service.clearSession();

    expect(tenantClear).toHaveBeenCalledTimes(1);
    expect(brandClear).toHaveBeenCalledTimes(1);
  });

  it('logout() clears tenant and brand context state before the request even resolves', () => {
    const tenantClear = vi.spyOn(tenantService, 'clear');
    const brandClear = vi.spyOn(brandContextService, 'clear');

    service.logout().subscribe();

    // clearSession() runs synchronously inside logout(), ahead of the HTTP call settling.
    expect(tenantClear).toHaveBeenCalledTimes(1);
    expect(brandClear).toHaveBeenCalledTimes(1);

    httpMock.expectOne(`${environment.apiUrl}/auth/logout`).flush({ status: 'success', data: true });
  });
});
