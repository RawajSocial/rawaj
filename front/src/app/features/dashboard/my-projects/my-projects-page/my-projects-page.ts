import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { TeamMemberService } from '../../../../services/team-member.service';
import { SeoService } from '../../../../services/seo.service';
import { ROLE_LABELS, STATUS_LABELS } from '../../../../model/team-member.model';

@Component({
  selector: 'app-my-projects-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './my-projects-page.html',
  styleUrls: ['../../dashboard-shared.css', './my-projects-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyProjectsPage {
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly seo = inject(SeoService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusLabels = STATUS_LABELS;

  protected readonly currentUser = this.teamMemberService.currentUser;

  protected readonly myProjects = computed(() => {
    const member = this.currentUser();
    if (!member) return [];
    const all = this.teamMemberService.projects();
    return all.filter(p => member.assignedProjectIds.includes(p.id));
  });

  constructor() {
    this.seo.setPageSeo({
      title: 'مشاريعي | رواج',
      description: 'المشاريع التي تمت دعوتك للعمل عليها ضمن فريق الوكالة.',
      keywords: 'رواج, مشاريعي, دعوات المشاريع, الفريق',
      path: '/dashboard/my-projects',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
