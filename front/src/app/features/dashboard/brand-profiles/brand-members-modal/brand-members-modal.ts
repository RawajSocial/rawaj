import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { ModalShell } from '../../../../shared/components/modal-shell/modal-shell';
import { TeamMemberService } from '../../../../services/team-member.service';
import { ROLE_LABELS, TeamMember } from '../../../../model/team-member.model';
import { ApiError } from '../../../../core/api';
import { BrandProfileSummary } from '../../../../core/models';

@Component({
  selector: 'app-brand-members-modal',
  imports: [ModalShell],
  templateUrl: './brand-members-modal.html',
  styleUrls: ['../../dashboard-shared.css', './brand-members-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandMembersModal {
  private readonly teamMemberService = inject(TeamMemberService);

  readonly open = input(false);
  readonly brand = input<BrandProfileSummary | null>(null);

  readonly closed = output<void>();

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly members = this.teamMemberService.members;
  protected readonly loading = this.teamMemberService.loading;
  protected readonly togglingId = signal<string | null>(null);
  protected readonly toggleError = signal<string | null>(null);

  protected readonly activeMembers = computed(() => this.members().filter((m) => m.invitationStatus === 'Accepted'));

  constructor() {
    effect(() => {
      if (this.open()) {
        this.toggleError.set(null);
        this.teamMemberService.load();
      }
    });
  }

  protected hasBrandAccess(member: TeamMember): boolean {
    return member.role === 'Owner' || member.role === 'Admin' || member.brandProfileIds.includes(this.brand()?.brandProfileId ?? '');
  }

  protected isToggleable(member: TeamMember): boolean {
    return member.role === 'Editor' || member.role === 'Viewer';
  }

  protected toggleAccess(member: TeamMember): void {
    const brandId = this.brand()?.brandProfileId;
    if (!brandId || !this.isToggleable(member)) return;

    const has = member.brandProfileIds.includes(brandId);
    const nextBrandIds = has
      ? member.brandProfileIds.filter((id) => id !== brandId)
      : [...member.brandProfileIds, brandId];

    this.togglingId.set(member.id);
    this.toggleError.set(null);
    this.teamMemberService.update(member.id, member.role, nextBrandIds).subscribe({
      next: () => this.togglingId.set(null),
      error: (error: unknown) => {
        this.togglingId.set(null);
        this.toggleError.set(
          error instanceof ApiError ? error.message : 'تعذر تحديث صلاحية الوصول لهذا العضو.',
        );
      },
    });
  }
}
