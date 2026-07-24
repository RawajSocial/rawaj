import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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

  protected readonly campaignId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly campaign = this.campaignService.getById(this.campaignId);
  protected readonly platformMeta = PLATFORM_META;
  protected readonly dayNames = DAY_NAMES;

  private readonly campaignPosts = this.scheduledPostService.byCampaign(this.campaignId);

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
    const c = this.campaign();
    this.seo.setPageSeo({
      title: 'تقويم ' + (c ? c.name : 'الحملة') + ' | رواج',
      description: 'تقويم منشورات الحملة المجدولة والمنشورة.',
      keywords: 'رواج, تقويم الحملة, جدولة المنشورات',
      path: '/dashboard/campaigns/' + this.campaignId + '/calendar',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
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
