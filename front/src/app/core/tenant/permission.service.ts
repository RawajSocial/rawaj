import { Injectable, computed, inject } from '@angular/core';
import { TenantService } from './tenant.service';
import { TenantMemberRole } from '../../model/tenant.model';

/** Mirrors Rawaj.Domain.Enums.TenantMemberRoleExtensions.Ranks on the backend — keep in sync. */
const ROLE_RANK: Record<TenantMemberRole, number> = { Viewer: 0, Editor: 1, Admin: 2, Owner: 3 };

/**
 * UX-only permission surface, computed off the current tenant role. Every action this hides or
 * disables is ALSO enforced server-side by TenantAuthorizationBehavior / BrandAccessAuthorization
 * Behavior — this exists so a Viewer isn't offered buttons that will just 403, never as a security
 * boundary in its own right.
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly tenantService = inject(TenantService);

  /** Prefers the membership matching the ACTIVE tenant: `tenant()` can lag one request behind a
   *  `switchTenant()` call (which fires `refresh()` asynchronously), and gating on a stale role
   *  would briefly show the wrong controls right after an org switch. */
  readonly role = computed<TenantMemberRole | null>(() => {
    const activeId = this.tenantService.activeTenantId();
    const membership = this.tenantService.memberships().find(m => m.tenantId === activeId);
    return membership?.role ?? this.tenantService.tenant()?.role ?? null;
  });

  private readonly rank = computed(() => {
    const r = this.role();
    return r ? ROLE_RANK[r] : -1; // unknown role => deny, never default-allow
  });

  readonly canView = computed(() => this.rank() >= ROLE_RANK.Viewer);
  readonly canEdit = computed(() => this.rank() >= ROLE_RANK.Editor);
  readonly canAdmin = computed(() => this.rank() >= ROLE_RANK.Admin);

  /** Empty when allowed; otherwise the Arabic explanation to surface in a tooltip. */
  readonly editDeniedReason = computed(() =>
    this.canEdit() ? '' : 'صلاحيتك الحالية (مشاهد) لا تسمح بهذا الإجراء — اطلب من مالك الحساب ترقيتك إلى محرّر.');
  readonly adminDeniedReason = computed(() =>
    this.canAdmin() ? '' : 'ربط الحسابات وفصلها متاح لمديري الحساب فقط.');

  hasAtLeast(min: TenantMemberRole): boolean {
    return this.rank() >= ROLE_RANK[min];
  }
}
