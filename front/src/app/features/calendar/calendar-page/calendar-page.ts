import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { ScheduledPost } from '../../../model/scheduled-post.model';
import { CAMPAIGN_PLATFORM_META, CampaignPlatform } from '../../../model/campaign.model';
import { PostModal } from '../post-modal/post-modal';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { BrandLock } from '../../../shared/components/brand-lock/brand-lock';
import { SeoService } from '../../../services/seo.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { ContentItemService } from '../../../services/content-item.service';
import { CampaignService } from '../../../services/campaign.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { PermissionService } from '../../../core/tenant/permission.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { Router, RouterLink } from '@angular/router';

export type ViewMode = 'month' | 'week' | 'day' | 'list';

/** Shared with the campaign card/detail/calendar/post pages and the ads module — see
 *  CAMPAIGN_PLATFORM_META. Each of those carried its own identical copy until now. */
const PLATFORM_CFG = CAMPAIGN_PLATFORM_META;

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [PostModal, PageHeader, BrandLock, RouterLink],
  templateUrl: './calendar-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './calendar-page.css'],
})
export class CalendarPage {
  private readonly seo = inject(SeoService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly contentItemService = inject(ContentItemService);
  private readonly campaignService = inject(CampaignService);
  protected readonly brandContextService = inject(BrandContextService);
  private readonly tenantService = inject(TenantService);
  protected readonly perms = inject(PermissionService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly router = inject(Router);

  constructor() {
    this.seo.setPageSeo({
      title: 'تقويم المحتوى | رواج',
      description: 'جدول ونظّم منشوراتك على جميع منصات التواصل الاجتماعي من تقويم واحد.',
      keywords: 'رواج, تقويم المحتوى, جدولة المنشورات, تخطيط النشر',
      path: '/dashboard/calendar',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    // The header is the single source of truth for brand/campaign — refetch
    // scheduled posts whenever either global selection changes.
    effect(() => {
      const brandId = this.brandContextService.selectedBrandProfileId();
      const campaignId = this.brandContextService.selectedCampaignId();
      if (!brandId) return;
      this.scheduledPostService.refresh(brandId, campaignId).subscribe();
      // Also pull the same brand/campaign's content items so the "unscheduled approved
      // content" nudge below can tell approved-but-never-scheduled drafts apart from
      // genuinely empty campaigns.
      this.contentItemService.refresh(brandId, campaignId).subscribe();
    });

    // Jump the visible month to whichever campaign is selected's own start date — switching
    // between campaigns should show that campaign's schedule, not always the current month.
    effect(() => {
      const campaignId = this.brandContextService.selectedCampaignId();
      if (campaignId === 'all') {
        this.currentDate.set(new Date());
        return;
      }
      const campaign = this.campaignService.getById(campaignId)();
      const parsed = campaign?.startDate ? new Date(campaign.startDate) : null;
      this.currentDate.set(parsed && !isNaN(parsed.getTime()) ? parsed : new Date());
    });
  }

  readonly viewMode       = signal<ViewMode>('month');
  readonly currentDate    = signal(new Date());
  readonly campaignOpen   = signal(false);
  readonly selectedPost   = signal<ScheduledPost | null>(null);
  readonly selectedDay    = signal<Date | null>(null);
  readonly posts          = this.scheduledPostService.posts;
  readonly brandProfileCount = this.tenantService.brandProfileCount;

  readonly platformCfg  = PLATFORM_CFG;
  /** This brand's campaigns, sourced from the header's global selection — kept
   *  as a page-local quick-filter chip, but it reads/writes the same global
   *  BrandContextService state (no separate page-local filter state). */
  readonly campaigns = computed(() => {
    const bp = this.brandContextService.selectedBrandProfileId();
    return bp ? this.campaignService.byBrandProfile(bp)() : [];
  });
  readonly campaignFilter = this.brandContextService.selectedCampaignId;
  readonly dayNames      = ['أحد', 'إثنين', 'ثلاثاء', 'أربعاء', 'خميس', 'جمعة', 'سبت'];

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="campaign"]')) this.campaignOpen.set(false);
  }

  // ── Filtered posts ──────────────────────────────────────────────────────

  readonly filteredPosts = computed<ScheduledPost[]>(() => {
    const cf = this.campaignFilter();
    return this.posts().filter(p => cf === 'all' || p.campaignId === cf);
  });

  // ── Unscheduled approved content nudge ──────────────────────────────────
  // Content items only appear on this calendar once explicitly scheduled — a fresh
  // marketing plan with approved-but-unscheduled drafts otherwise looks like an empty
  // calendar with no explanation. Surface that gap directly.
  readonly unscheduledApprovedCount = computed<number>(() => {
    const scheduledContentItemIds = new Set(this.posts().map(p => p.contentItemId));
    return this.contentItemService.items()
      .filter(item => item.status === 'Approved' && !scheduledContentItemIds.has(item.contentItemId))
      .length;
  });

  /** Only meaningful when a single campaign is selected — the nudge's "schedule now" link
   *  targets that campaign's content page. With "all campaigns" selected there's no single
   *  target, so the link is omitted and the count is shown alone. */
  readonly unscheduledNudgeCampaignId = computed<string | null>(() => {
    const cf = this.campaignFilter();
    return cf !== 'all' ? cf : null;
  });

  readonly postsByDay = computed<Map<string, ScheduledPost[]>>(() => {
    const map = new Map<string, ScheduledPost[]>();
    for (const p of this.filteredPosts()) {
      const key = p.scheduledAt.substring(0, 10);
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(p);
    }
    return map;
  });

  // ── Month grid ──────────────────────────────────────────────────────────

  readonly calendarWeeks = computed<Date[][]>(() => {
    const d     = this.currentDate();
    const year  = d.getFullYear();
    const month = d.getMonth();
    const first = new Date(year, month, 1);
    const last  = new Date(year, month + 1, 0);
    const offset = first.getDay(); // Sunday = 0
    const cur   = new Date(year, month, 1 - offset);
    const weeks: Date[][] = [];
    do {
      const week: Date[] = [];
      for (let c = 0; c < 7; c++) {
        week.push(new Date(cur));
        cur.setDate(cur.getDate() + 1);
      }
      weeks.push(week);
    } while (cur <= last);
    return weeks;
  });

  // ── Week days ───────────────────────────────────────────────────────────

  readonly weekDays = computed<Date[]>(() => {
    const d   = this.currentDate();
    const dow = d.getDay();
    const sun = new Date(d.getFullYear(), d.getMonth(), d.getDate() - dow);
    return Array.from({ length: 7 }, (_, i) => new Date(sun.getFullYear(), sun.getMonth(), sun.getDate() + i));
  });

  // ── List groups (current week) ──────────────────────────────────────────

  readonly listGroups = computed<{ dateLabel: string; date: Date; posts: ScheduledPost[] }[]>(() => {
    const days   = this.weekDays();
    const startT = days[0].getTime();
    const endT   = new Date(days[6].getFullYear(), days[6].getMonth(), days[6].getDate(), 23, 59, 59).getTime();
    const map    = new Map<string, { dateLabel: string; date: Date; posts: ScheduledPost[] }>();

    for (const p of this.filteredPosts()) {
      const pd = new Date(p.scheduledAt);
      if (pd.getTime() < startT || pd.getTime() > endT) continue;
      const key = p.scheduledAt.substring(0, 10);
      if (!map.has(key)) {
        map.set(key, {
          date: pd,
          dateLabel: pd.toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }),
          posts: [],
        });
      }
      map.get(key)!.posts.push(p);
    }
    return [...map.values()].sort((a, b) => a.date.getTime() - b.date.getTime());
  });

  // ── Header title ────────────────────────────────────────────────────────

  readonly headerTitle = computed<string>(() => {
    const d    = this.currentDate();
    const mode = this.viewMode();
    if (mode === 'month') {
      return d.toLocaleDateString('ar-EG', { month: 'long', year: 'numeric' });
    }
    if (mode === 'day') {
      return d.toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    }
    const days = this.weekDays();
    const sf = days[0].toLocaleDateString('ar-EG', { day: 'numeric', month: 'short' });
    const ef = days[6].toLocaleDateString('ar-EG', { day: 'numeric', month: 'short', year: 'numeric' });
    return `${sf} – ${ef}`;
  });

  // ── Navigation ──────────────────────────────────────────────────────────

  prev(): void { this.moveDate(-1); }
  next(): void { this.moveDate(1); }

  goToday(): void { this.currentDate.set(new Date()); }

  private moveDate(dir: 1 | -1): void {
    const d = new Date(this.currentDate());
    const m = this.viewMode();
    if (m === 'month')     d.setMonth(d.getMonth() + dir);
    else if (m === 'day')  d.setDate(d.getDate() + dir);
    else                   d.setDate(d.getDate() + dir * 7);
    this.currentDate.set(d);
  }

  // ── Day helpers ─────────────────────────────────────────────────────────

  postsForDay(date: Date): ScheduledPost[] {
    const key = this.toDateStr(date);
    return this.postsByDay().get(key) ?? [];
  }

  private toDateStr(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  isToday(date: Date): boolean {
    const t = new Date();
    return date.getDate() === t.getDate() && date.getMonth() === t.getMonth() && date.getFullYear() === t.getFullYear();
  }

  isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.currentDate().getMonth();
  }

  isSelectedDay(date: Date): boolean {
    const sel = this.selectedDay();
    return !!sel
      && date.getDate() === sel.getDate()
      && date.getMonth() === sel.getMonth()
      && date.getFullYear() === sel.getFullYear();
  }

  selectDay(date: Date): void {
    this.selectedDay.set(this.isSelectedDay(date) ? null : new Date(date));
  }

  // ── Side panel (selected day + upcoming) ────────────────────────────────

  readonly selectedDayPosts = computed<ScheduledPost[]>(() => {
    const day = this.selectedDay();
    return day ? this.postsForDay(day) : [];
  });

  readonly selectedDayLabel = computed<string>(() => {
    const day = this.selectedDay();
    return day ? day.toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }) : '';
  });

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  goToDay(date: Date): void {
    this.currentDate.set(new Date(date));
    this.viewMode.set('day');
  }

  get campaignLabel(): string {
    if (this.campaignFilter() === 'all') return 'جميع الحملات';
    return this.campaigns().find(c => c.id === this.campaignFilter())?.name ?? 'جميع الحملات';
  }

  setCampaign(id: string): void {
    this.brandContextService.setCampaign(id);
    this.campaignOpen.set(false);
  }

  // ── Post modal ──────────────────────────────────────────────────────────

  openPost(post: ScheduledPost, event?: Event): void {
    event?.stopPropagation();
    this.selectedPost.set(post);
  }

  closeModal(): void { this.selectedPost.set(null); }

  reschedulePost(event: { id: string; scheduledAt: string }): void {
    if (!this.requireBrandProfile()) return;
    this.scheduledPostService.reschedule(event.id, event.scheduledAt).subscribe({
      next: () => this.selectedPost.set(null),
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تحديث موعد النشر.'), { variant: 'error' }),
    });
  }

  publishPostNow(id: string): void {
    if (!this.requireBrandProfile()) return;
    this.scheduledPostService.publishNow(id).subscribe({
      next: () => this.selectedPost.set(null),
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر نشر المنشور الآن.'), { variant: 'error' }),
    });
  }

  deletePost(id: string): void {
    if (!this.requireBrandProfile()) return;
    this.scheduledPostService.cancel(id).subscribe({
      next: () => {
        this.scheduledPostService.remove(id);
        this.selectedPost.set(null);
      },
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إلغاء جدولة المنشور.'), { variant: 'error' }),
    });
  }

  private requireBrandProfile(): boolean {
    if (this.tenantService.brandProfileCount() > 0) return true;
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    this.router.navigate(['/dashboard/brand-profiles/new']);
    return false;
  }
}
