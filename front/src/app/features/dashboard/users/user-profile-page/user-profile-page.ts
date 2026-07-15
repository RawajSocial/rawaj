import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TeamMemberService } from '../../../../services/team-member.service';
import { ROLE_LABELS } from '../../../../model/team-member.model';

@Component({
  selector: 'app-user-profile-page',
  imports: [RouterLink],
  templateUrl: './user-profile-page.html',
  styleUrls: ['../users-shared.css', './user-profile-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserProfilePage {
  private readonly route = inject(ActivatedRoute);
  private readonly teamMemberService = inject(TeamMemberService);

  protected readonly roleLabels = ROLE_LABELS;

  private readonly memberId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly member = this.teamMemberService.getById(this.memberId);
  protected readonly loading = this.teamMemberService.loading;

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
    return this.member()?.joinedAt ? 'نشط' : 'بانتظار القبول';
  }
}
