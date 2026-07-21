import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { TeamMember } from '../model/team-member.model';
import { TeamMembersApiService } from '../core/api/team-members-api.service';
import { TenantMemberRole } from '../core/models';

@Injectable({ providedIn: 'root' })
export class TeamMemberService {
  private readonly api = inject(TeamMembersApiService);

  private readonly _members = signal<TeamMember[]>([]);
  readonly members = this._members.asReadonly();
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly activeCount = computed(() => this._members().filter(m => m.invitationStatus === 'Accepted').length);
  readonly pendingCount = computed(() => this._members().filter(m => m.invitationStatus === 'Pending').length);

  getById(id: string) {
    return computed(() => this._members().find(m => m.id === id));
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.api.getAll().subscribe({
      next: (members) => {
        this._members.set(
          members.map((m) => ({
            id: m.tenantMemberId,
            userId: m.userId,
            name: m.fullName,
            email: m.email,
            role: m.role,
            invitationStatus: m.invitationStatus,
            joinedAt: m.joinedAt,
            brandProfileIds: m.brandProfileIds,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set('تعذر تحميل أعضاء الفريق.');
      },
    });
  }

  invite(email: string, role: TenantMemberRole, brandProfileIds: string[]): Observable<unknown> {
    return this.api.add({ email, role, brandProfileIds }).pipe(tap(() => this.load()));
  }

  update(tenantMemberId: string, role: TenantMemberRole, brandProfileIds: string[]): Observable<unknown> {
    return this.api.update(tenantMemberId, { role, brandProfileIds }).pipe(tap(() => this.load()));
  }

  remove(tenantMemberId: string): Observable<unknown> {
    return this.api.remove(tenantMemberId).pipe(tap(() => this.load()));
  }
}
