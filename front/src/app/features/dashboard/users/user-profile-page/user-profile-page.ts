import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TeamMemberService } from '../../../../services/team-member.service';
import { ROLE_LABELS } from '../../../../model/team-member.model';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { ApiError } from '../../../../core/api';
import { UserFormModal, UserFormValue, UserUpdateValue } from '../user-form-modal/user-form-modal';

@Component({
  selector: 'app-user-profile-page',
  imports: [RouterLink, UserFormModal],
  templateUrl: './user-profile-page.html',
  styleUrls: ['../users-shared.css', './user-profile-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly tenantService = inject(TenantService);

  protected readonly roleLabels = ROLE_LABELS;

  private readonly memberId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly member = this.teamMemberService.getById(this.memberId);
  protected readonly loading = this.teamMemberService.loading;

  protected readonly canManageMembers = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin';
  });

  protected readonly brandNames = computed(() => {
    const ids = new Set(this.member()?.brandProfileIds ?? []);
    return this.tenantService.brandProfiles()
      .filter((b) => ids.has(b.brandProfileId))
      .map((b) => b.name);
  });

  protected readonly formModalOpen = signal(false);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly removing = signal(false);

  constructor() {
    // Direct navigation to this URL (e.g. a refresh) may land here before the list has loaded.
    effect(() => {
      if (this.teamMemberService.members().length === 0 && !this.teamMemberService.loading()) {
        this.teamMemberService.load();
      }
    });
  }

  protected initials(name: string): string {
    return name.trim().split(/\s+/).slice(0, 2).map(p => p[0]).join('');
  }

  protected statusLabel(): string {
    switch (this.member()?.invitationStatus) {
      case 'Accepted': return 'نشط';
      case 'Declined': return 'مرفوضة';
      default: return 'بانتظار القبول';
    }
  }

  protected openEdit(): void {
    this.saveError.set(null);
    this.formModalOpen.set(true);
  }

  protected closeFormModal(): void {
    this.formModalOpen.set(false);
  }

  // The modal only ever emits `update` here since `editingMember` is always set on this page.
  protected onIgnoreCreateSave(_value: UserFormValue): void {}

  protected onMemberUpdated(value: UserUpdateValue): void {
    const member = this.member();
    if (!member) return;

    this.saving.set(true);
    this.saveError.set(null);

    this.teamMemberService.update(member.id, value.role, value.brandProfileIds).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeFormModal();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.saveError.set(error instanceof ApiError ? error.message : 'تعذر حفظ التغييرات.');
      },
    });
  }

  protected removeMember(): void {
    const member = this.member();
    if (!member) return;
    if (!window.confirm(`هل أنت متأكد من إزالة ${member.name} من الفريق؟`)) return;

    this.removing.set(true);
    this.teamMemberService.remove(member.id).subscribe({
      next: () => {
        this.removing.set(false);
        void this.router.navigate(['/dashboard/users']);
      },
      error: (error: unknown) => {
        this.removing.set(false);
        this.saveError.set(error instanceof ApiError ? error.message : 'تعذر إزالة العضو.');
      },
    });
  }
}
