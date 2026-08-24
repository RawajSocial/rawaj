import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, switchMap, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { TenantMemberRole } from '../model/tenant.model';
import {
  AcceptInvitationAndRegisterRequest,
  AcceptInvitationAndRegisterResponse,
  AddTeamMemberRequest,
  AddTeamMemberResponse,
  AllocateCoinsResponse,
  InvitationDetailsResponse,
  PendingInvitationSummary,
  PendingInviteSummary,
  TeamActivityPageResponse,
  TeamMemberSummary,
  UpdateTeamMemberRequest,
  UpdateTeamMemberResponse,
} from '../model/team-member.model';

/** Real HTTP-backed team/collaboration service — replaces the old in-memory mock. All list state
 *  is tenant-scoped (the active `X-Tenant-Id` is attached by `tenantInterceptor`), so callers must
 *  `refresh()` again after switching tenants. */
@Injectable({ providedIn: 'root' })
export class TeamMemberService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/team-members`;

  private readonly _members = signal<TeamMemberSummary[]>([]);
  readonly members = this._members.asReadonly();

  private readonly _pendingInvitations = signal<PendingInvitationSummary[]>([]);
  readonly pendingInvitations = this._pendingInvitations.asReadonly();

  private readonly _myPendingInvites = signal<PendingInviteSummary[]>([]);
  readonly myPendingInvites = this._myPendingInvites.asReadonly();

  readonly acceptedCount = computed(() => this._members().filter(m => m.invitationStatus === 'Accepted').length);
  readonly pendingCount = computed(
    () => this._members().filter(m => m.invitationStatus === 'Pending').length + this._pendingInvitations().length,
  );

  getById(id: string) {
    return computed(() => this._members().find(m => m.tenantMemberId === id));
  }

  /** Refreshes the member list and the admin's pending-invitation (no-account-yet) list together,
   *  since the users-page needs both to render "الكل" without a flash of missing rows. */
  refresh(): Observable<[ApiResponse<TeamMemberSummary[]>, ApiResponse<PendingInvitationSummary[]>]> {
    const members$ = this.http.get<ApiResponse<TeamMemberSummary[]>>(this.baseUrl).pipe(
      tap(res => {
        if (res.data) this._members.set(res.data);
      }),
    );
    const invitations$ = this.http.get<ApiResponse<PendingInvitationSummary[]>>(`${this.baseUrl}/invitations`).pipe(
      tap(res => {
        if (res.data) this._pendingInvitations.set(res.data);
      }),
    );
    return new Observable(subscriber => {
      let a: ApiResponse<TeamMemberSummary[]> | undefined;
      let b: ApiResponse<PendingInvitationSummary[]> | undefined;
      const done = () => {
        if (a && b) {
          subscriber.next([a, b]);
          subscriber.complete();
        }
      };
      members$.subscribe({
        next: r => { a = r; done(); },
        error: e => subscriber.error(e),
      });
      invitations$.subscribe({
        next: r => { b = r; done(); },
        error: e => subscriber.error(e),
      });
    });
  }

  /** Invites addressed to the current signed-in user (across every tenant) — "you've been invited". */
  refreshMyPendingInvites(): Observable<ApiResponse<PendingInviteSummary[]>> {
    return this.http.get<ApiResponse<PendingInviteSummary[]>>(`${this.baseUrl}/pending-invites`).pipe(
      tap(res => {
        if (res.data) this._myPendingInvites.set(res.data);
      }),
    );
  }

  private mutateAndRefresh<T>(mutation: Observable<ApiResponse<T>>): Observable<ApiResponse<T>> {
    return mutation.pipe(
      switchMap(res => (res.data ? this.refresh().pipe(map(() => res)) : of(res))),
    );
  }

  invite(request: AddTeamMemberRequest): Observable<ApiResponse<AddTeamMemberResponse>> {
    return this.mutateAndRefresh(this.http.post<ApiResponse<AddTeamMemberResponse>>(this.baseUrl, request));
  }

  updateMember(tenantMemberId: string, request: UpdateTeamMemberRequest): Observable<ApiResponse<UpdateTeamMemberResponse>> {
    return this.mutateAndRefresh(
      this.http.put<ApiResponse<UpdateTeamMemberResponse>>(`${this.baseUrl}/${tenantMemberId}`, request),
    );
  }

  allocateCoins(tenantMemberId: string, newAllocation: number): Observable<ApiResponse<AllocateCoinsResponse>> {
    return this.mutateAndRefresh(
      this.http.post<ApiResponse<AllocateCoinsResponse>>(`${this.baseUrl}/${tenantMemberId}/coins`, { newAllocation }),
    );
  }

  removeMember(tenantMemberId: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}`));
  }

  revokeInvitation(invitationId: string): Observable<ApiResponse<boolean>> {
    return this.mutateAndRefresh(this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/invitations/${invitationId}`));
  }

  acceptInvite(tenantMemberId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}/accept`, {});
  }

  declineInvite(tenantMemberId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${tenantMemberId}/decline`, {});
  }

  getActivity(page = 1, pageSize = 20, userId?: string): Observable<ApiResponse<TeamActivityPageResponse>> {
    let url = `${this.baseUrl}/activity?page=${page}&pageSize=${pageSize}`;
    if (userId) url += `&userId=${encodeURIComponent(userId)}`;
    return this.http.get<ApiResponse<TeamActivityPageResponse>>(url);
  }

  /** Anonymous — backs the public `/invite?token=...` page, works for both invite kinds. */
  getInvitationDetails(token: string): Observable<ApiResponse<InvitationDetailsResponse>> {
    return this.http.get<ApiResponse<InvitationDetailsResponse>>(`${this.baseUrl}/invitations/${encodeURIComponent(token)}`);
  }

  /** Anonymous — registers a brand-new invitee and accepts the invite in one call. */
  acceptInvitationAndRegister(
    token: string,
    request: Omit<AcceptInvitationAndRegisterRequest, 'token'>,
  ): Observable<ApiResponse<AcceptInvitationAndRegisterResponse>> {
    return this.http.post<ApiResponse<AcceptInvitationAndRegisterResponse>>(
      `${this.baseUrl}/invitations/${encodeURIComponent(token)}/accept`,
      request,
    );
  }

  roleLabel(role: TenantMemberRole): string {
    return { Owner: 'مالك', Admin: 'مدير', Editor: 'محرر', Viewer: 'مشاهد' }[role];
  }
}
