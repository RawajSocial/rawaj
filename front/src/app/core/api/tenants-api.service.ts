import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, CreateTenantRequest, CreateTenantResponse, MyTenant } from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class TenantsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/tenants`;

  create(request: CreateTenantRequest): Observable<CreateTenantResponse> {
    return this.http.post<ApiResponse<CreateTenantResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getMine(): Observable<MyTenant> {
    return this.http.get<ApiResponse<MyTenant>>(`${this.baseUrl}/me`).pipe(unwrapApiResponse());
  }
}
