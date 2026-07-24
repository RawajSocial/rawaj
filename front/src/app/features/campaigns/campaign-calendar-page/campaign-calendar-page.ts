import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { CampaignPlatform } from '../../../model/campaign.model';
import { ScheduledPost } from '../../../model/scheduled-post.model';
import { SeoService } from '../../../services/seo.service';

interface PlatformMeta {
  icon: string;
  color: string;
}

const PLATFORM_META: Record<CampaignPlatform, PlatformMeta> = {
  instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)' },
  facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)' },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)' },
  youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)' },
  x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)' },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)' },
};

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
  protected readonly campaign = computed(() => this.campaignService.getById(this.campaignId())());
  protected readonly platformMeta = PLATFORM_META;
  protected readonly dayNames = DAY_NAMES;

  private readonly campaignPosts = computed(() => this.scheduledPostService.byCampaign(this.campaignId())());

  // Defaults to the campaign's own start date (if it has one and it isn't in the past) so the
  // calendar opens showing the campaign's actual schedule, not always "today".
  protected readonly currentDate = signal(this.resolveInitialDate());

  private resolveInitialDate(): Date {
    const start = this.campaign()?.startDate;
    if (start) {
      const parsed = new Date(start);
      if (!isNaN(parsed.getTime())) return parsed;
    }
    return new Date();
  }

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
    this.currentDate().toLocaleDateString('ar-SA', { month: 'long', year: 'numeric' }),
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

    // ScheduledPostService's list is otherwise only ever populated by /dashboard/calendar — without
    // this, this page's grid (and campaign-detail's "upcoming posts") always renders empty.
    effect(() => {
      const brandProfileId = this.campaign()?.brandProfileId;
      const id = this.campaignId();
      if (brandProfileId) this.scheduledPostService.refresh(brandProfileId, id).subscribe();
    });

    // Jump the visible month back to the (new) campaign's own start date whenever the route's
    // campaign id actually changes — not on every unrelated data refresh.
    effect(() => {
      this.campaignId();
      untracked(() => this.currentDate.set(this.resolveInitialDate()));
    });
  }

  protected prev(): void { this.moveMonth(-1); }
  protected next(): void { this.moveMonth(1); }
  protected goToday(): void { this.currentDate.set(new Date()); }

  private moveMonth(dir: 1 | -1): void {
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
