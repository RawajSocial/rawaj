import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddTeamMemberRequest, AddTeamMemberResponse, ApiResponse, TeamMemberSummary } from '../models';
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
}
