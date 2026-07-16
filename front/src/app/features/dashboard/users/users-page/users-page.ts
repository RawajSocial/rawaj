import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { animate, stagger } from 'motion';
import { TeamMemberService } from '../../../../services/team-member.service';
import { ROLE_LABELS, STATUS_LABELS, TeamMember, TeamMemberStatus } from '../../../../model/team-member.model';
import { UserFormModal, UserFormValue } from '../user-form-modal/user-form-modal';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';

type StatusTab = 'all' | TeamMemberStatus;

@Component({
  selector: 'app-users-page',
  imports: [RouterLink, UserFormModal, PageHeader],
  templateUrl: './users-page.html',
  styleUrls: ['../../../campaigns/campaigns-page/campaigns-page.css', './users-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly seo = inject(SeoService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly statusLabels = STATUS_LABELS;

  protected readonly members = this.teamMemberService.members;
  protected readonly searchQuery = signal('');
  protected readonly statusTab = signal<StatusTab>('all');
  protected readonly inviteModalOpen = signal(false);

  protected readonly tabs: { value: StatusTab; label: string }[] = [
    { value: 'all', label: 'الكل' },
    { value: 'active', label: 'نشط' },
    { value: 'pending', label: 'بانتظار القبول' },
    { value: 'suspended', label: 'موقوف' },
  ];

  protected readonly filtered = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const tab = this.statusTab();
    return this.members().filter(m => {
      if (tab !== 'all' && m.status !== tab) return false;
      if (q && !m.name.toLowerCase().includes(q) && !m.email.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  private readonly rowRefs = viewChildren<ElementRef<HTMLElement>>('row');

  constructor() {
    this.seo.setPageSeo({
      title: 'المستخدمون | رواج',
      description: 'ادعُ أعضاء فريقك، وزّع المهام، وتابع صلاحياتهم من مكان واحد.',
      keywords: 'رواج, المستخدمون, إدارة الفريق, صلاحيات',
      path: '/dashboard/users',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
    afterNextRender(() => this.animateRows());
  }

  protected initials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map(p => p[0])
      .join('');
  }

  protected setTab(tab: StatusTab): void {
    this.statusTab.set(tab);
    queueMicrotask(() => this.animateRows());
  }

  protected onSearch(value: string): void {
    this.searchQuery.set(value);
    queueMicrotask(() => this.animateRows());
  }

  protected openInvite(): void {
    this.inviteModalOpen.set(true);
  }

  protected onInviteSaved(value: UserFormValue): void {
    this.teamMemberService.inviteMember(value);
    this.inviteModalOpen.set(false);
    queueMicrotask(() => this.animateRows());
  }

  protected removeMember(id: string, event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.teamMemberService.removeMember(id);
  }

  protected trackById(_index: number, member: TeamMember): string {
    return member.id;
  }

  private animateRows(): void {
    const rows = this.rowRefs().map(r => r.nativeElement);
    if (!rows.length) return;
    animate(
      rows,
      { opacity: [0, 1], transform: ['translateY(8px)', 'translateY(0)'] },
      { duration: 0.32, delay: stagger(0.035), ease: [0.16, 1, 0.3, 1] },
    );
  }
}
