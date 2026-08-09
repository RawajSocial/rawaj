import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { CAMPAIGN_PLATFORM_META, GetCampaignResponse } from '../../../model/campaign.model';
import { ScheduledPost } from '../../../model/scheduled-post.model';
import { SeoService } from '../../../services/seo.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

const DAY_NAMES = ['أحد', 'إثنين', 'ثلاثاء', 'أربعاء', 'خميس', 'جمعة', 'سبت'];

@Component({
  selector: 'app-campaign-calendar-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './campaign-calendar-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-calendar-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignCalendarPage {
  private readonly route = inject(ActivatedRoute);
  private readonly campaignService = inject(CampaignService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly seo = inject(SeoService);

  /// Reactive route param — the router reuses this component instance across navigations that
  /// only change `:id` (e.g. jumping from one campaign's calendar straight to another's), so a
  /// snapshot read here would freeze on the first campaign forever.
  private readonly paramMap = toSignal(this.route.paramMap, { requireSync: true });
  protected readonly campaignId = computed(() => this.paramMap().get('id') ?? '');

  /** Fetched directly rather than looked up in CampaignService's list. The list is only ever
   *  loaded once by UserLayout, so reading it here made this page render "الحملة غير موجودة" for
   *  a campaign that exists — on every deep link/reload until the list happened to arrive, and
   *  permanently for a campaign the list doesn't carry (archived ones, or a tenant with more
   *  campaigns than the list's page size). */
  protected readonly campaign = signal<GetCampaignResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly platformMeta = CAMPAIGN_PLATFORM_META;
  protected readonly dayNames = DAY_NAMES;

  private readonly campaignPosts = computed(() => this.scheduledPostService.byCampaign(this.campaignId())());

  // Defaults to the campaign's own start date so the calendar opens showing the campaign's actual
  // schedule, not always "today". Starts on today and is moved once the campaign actually arrives
  // (see the constructor) — the campaign is now fetched, so its start date isn't known at
  // construction time the way it was when this read a preloaded list.
  protected readonly currentDate = signal(new Date());

  /** True once the user has navigated the calendar themselves — after that, an arriving campaign
   *  must not yank the visible month back to the campaign's start date under them. */
  private monthPinnedByUser = false;

  protected readonly postsByDay = computed<Map<string, ScheduledPost[]>>(() => {
    const map = new Map<string, ScheduledPost[]>();
    for (const p of this.campaignPosts()) {
      const key = p.scheduledAt.substring(0, 10);
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(p);
    }
    return map;
  });

  protected readonly calendarWeeks = computed<Date[][]>(() => {
    const d = this.currentDate();
    const year = d.getFullYear();
    const month = d.getMonth();
    const first = new Date(year, month, 1);
    const last = new Date(year, month + 1, 0);
    const offset = first.getDay();
    const cur = new Date(year, month, 1 - offset);
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

  protected readonly headerTitle = computed(() =>
    this.currentDate().toLocaleDateString('ar-EG', { month: 'long', year: 'numeric' }),
  );

  constructor() {
    effect(() => {
      const c = this.campaign();
      const id = this.campaignId();
      this.seo.setPageSeo({
        title: 'تقويم ' + (c ? c.name : 'الحملة') + ' | رواج',
        description: 'تقويم منشورات الحملة المجدولة والمنشورة.',
        keywords: 'رواج, تقويم الحملة, جدولة المنشورات',
        path: '/dashboard/campaigns/' + id + '/calendar',
        image: '/home-hero-light.png',
        type: 'website',
        noIndex: true,
      });
    });

    // Re-fetches whenever the route's campaign id actually changes — the router reuses this
    // component instance across in-app navigation between two campaigns' calendars.
    effect(() => {
      const id = this.campaignId();
      untracked(() => this.loadCampaign(id));
    });

    // ScheduledPostService's list is otherwise only ever populated by /dashboard/calendar — without
    // this, this page's grid (and campaign-detail's "upcoming posts") always renders empty.
    effect(() => {
      const brandProfileId = this.campaign()?.brandProfileId;
      const id = this.campaignId();
      if (brandProfileId) this.scheduledPostService.refresh(brandProfileId, id).subscribe();
    });
  }

  private loadCampaign(id: string): void {
    this.campaign.set(null);
    this.loadError.set(null);
    this.monthPinnedByUser = false;
    this.currentDate.set(new Date());
    if (!id) {
      this.loading.set(false);
      this.loadError.set('لم يتم العثور على الحملة.');
      return;
    }

    this.loading.set(true);
    this.campaignService.getCampaign(id).subscribe({
      next: res => {
        this.loading.set(false);
        if (!res.data) {
          this.loadError.set('لم يتم العثور على الحملة.');
          return;
        }
        this.campaign.set(res.data);
        this.openOnCampaignStart(res.data.startDate);
      },
      error: err => {
        this.loading.set(false);
        this.loadError.set(extractApiErrorMessage(err, 'تعذّر تحميل بيانات الحملة.'));
      },
    });
  }

  /** Opens the grid on the campaign's own start month, unless the user has already navigated. */
  private openOnCampaignStart(startDate: string | null | undefined): void {
    if (this.monthPinnedByUser || !startDate) return;
    const parsed = new Date(startDate);
    if (!isNaN(parsed.getTime())) this.currentDate.set(parsed);
  }

  protected prev(): void { this.moveMonth(-1); }
  protected next(): void { this.moveMonth(1); }
  protected goToday(): void { this.monthPinnedByUser = true; this.currentDate.set(new Date()); }

  private moveMonth(dir: 1 | -1): void {
    this.monthPinnedByUser = true;
    const d = new Date(this.currentDate());
    d.setMonth(d.getMonth() + dir);
    this.currentDate.set(d);
  }

  protected postsForDay(date: Date): ScheduledPost[] {
    const key = this.toDateStr(date);
    return this.postsByDay().get(key) ?? [];
  }

  private toDateStr(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  protected isToday(date: Date): boolean {
    const t = new Date();
    return date.getDate() === t.getDate() && date.getMonth() === t.getMonth() && date.getFullYear() === t.getFullYear();
  }

  protected isCurrentMonth(date: Date): boolean {
    return date.getMonth() === this.currentDate().getMonth();
  }
}
