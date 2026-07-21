import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddTeamMemberRequest,
  AddTeamMemberResponse,
  ApiResponse,
  PendingInviteSummary,
  TeamMemberSummary,
  UpdateTeamMemberRequest,
  UpdateTeamMemberResponse,
} from '../models';
import { unwrapApiResponse } from './unwrap-api-response';

@Injectable({ providedIn: 'root' })
export class TeamMembersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/team-members`;

  add(request: AddTeamMemberRequest): Observable<AddTeamMemberResponse> {
    return this.http.post<ApiResponse<AddTeamMemberResponse>>(this.baseUrl, request).pipe(unwrapApiResponse());
  }

  getAll(): Observable<TeamMemberSummary[]> {
    return this.http.get<ApiResponse<TeamMemberSummary[]>>(this.baseUrl).pipe(unwrapApiResponse());
  }

  getMyPendingInvites(): Observable<PendingInviteSummary[]> {
    return this.http
      .get<ApiResponse<PendingInviteSummary[]>>(`${this.baseUrl}/pending-invites`)
      .pipe(unwrapApiResponse());
  }

  acceptInvite(tenantMemberId: string): Observable<boolean> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}/accept`, {})
      .pipe(unwrapApiResponse());
  }

  declineInvite(tenantMemberId: string): Observable<boolean> {
    return this.http
      .post<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}/decline`, {})
      .pipe(unwrapApiResponse());
  }

  update(tenantMemberId: string, request: UpdateTeamMemberRequest): Observable<UpdateTeamMemberResponse> {
    return this.http
      .put<ApiResponse<UpdateTeamMemberResponse>>(`${this.baseUrl}/${tenantMemberId}`, request)
      .pipe(unwrapApiResponse());
  }

  remove(tenantMemberId: string): Observable<boolean> {
    return this.http
      .delete<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}`)
      .pipe(unwrapApiResponse());
  }
}
