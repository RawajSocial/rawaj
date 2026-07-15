import { Component, HostListener, computed, effect, inject, signal } from '@angular/core';
import { ScheduledPost, PostStatus } from '../../../model/scheduled-post.model';
import { SocialPlatform, CampaignSummary } from '../../../core/models';
import { PostModal } from '../post-modal/post-modal';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { SchedulingApiService } from '../../../core/api/scheduling-api.service';
import { ContentApiService } from '../../../core/api/content-api.service';
import { CampaignsApiService } from '../../../core/api/campaigns-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';

export type ViewMode = 'month' | 'week' | 'day' | 'list';

const PLATFORM_CFG: Record<SocialPlatform, { icon: string; color: string; label: string }> = {
  Instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
  Facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
  Tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#222',    label: 'تيك توك'  },
  Youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
  Twitter:   { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
  Linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
};

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [PostModal, PageHeader],
  templateUrl: './calendar-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './calendar-page.css'],
})
export class CalendarPage {
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly contentApi = inject(ContentApiService);
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly tenantService = inject(TenantService);

  readonly viewMode       = signal<ViewMode>('month');
  readonly currentDate    = signal(new Date());
  readonly campaignFilter = signal<string>('all');
  readonly campaignOpen   = signal(false);
  readonly selectedPost   = signal<ScheduledPost | null>(null);
  readonly loadingDetail  = signal(false);
  readonly selectedDay    = signal<Date | null>(null);
  readonly posts          = signal<ScheduledPost[]>([]);
  readonly loading        = signal(false);
  readonly loadError      = signal<string | null>(null);
  readonly actionError    = signal<string | null>(null);

  readonly platformCfg  = PLATFORM_CFG;
  readonly campaigns    = signal<CampaignSummary[]>([]);
  readonly dayNames     = ['أحد', 'إثنين', 'ثلاثاء', 'أربعاء', 'خميس', 'جمعة', 'سبت'];

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.campaignsApi.getAll(brandProfileId, 1, 50).subscribe({
          next: (result) => this.campaigns.set(result.items),
        });
      }
    });

    effect(() => {
      const campaignId = this.campaignFilter();
      this.loadPosts(campaignId === 'all' ? undefined : campaignId);
    });
  }

  private loadPosts(campaignId: string | undefined): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.schedulingApi.getAll(campaignId, 1, 200).subscribe({
      next: (result) => {
        this.posts.set(
          result.items.map((p) => ({
            id: p.scheduledPostId,
            contentItemId: p.contentItemId,
            platform: p.platform,
            accountName: p.accountName,
            scheduledAt: p.scheduledAt,
            status: p.status,
            publishedAt: p.publishedAt,
            errorMessage: p.errorMessage,
          })),
        );
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل المنشورات المجدولة.');
      },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="campaign"]')) this.campaignOpen.set(false);
  }

  // ── Filtered posts (filtering already happened server-side by campaign) ──

  readonly filteredPosts = computed<ScheduledPost[]>(() => this.posts());

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
          dateLabel: pd.toLocaleDateString('ar-SA', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }),
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
      return d.toLocaleDateString('ar-SA', { month: 'long', year: 'numeric' });
    }
    if (mode === 'day') {
      return d.toLocaleDateString('ar-SA', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    }
    const days = this.weekDays();
    const sf = days[0].toLocaleDateString('ar-SA', { day: 'numeric', month: 'short' });
    const ef = days[6].toLocaleDateString('ar-SA', { day: 'numeric', month: 'short', year: 'numeric' });
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
    return day ? day.toLocaleDateString('ar-SA', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }) : '';
  });

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  goToDay(date: Date): void {
    this.currentDate.set(new Date(date));
    this.viewMode.set('day');
  }

  get campaignLabel(): string {
    return this.campaigns().find(c => c.campaignId === this.campaignFilter())?.name ?? 'جميع الحملات';
  }

  setCampaign(id: string): void {
    this.campaignFilter.set(id);
    this.campaignOpen.set(false);
  }

  // ── Post modal ──────────────────────────────────────────────────────────

  openPost(post: ScheduledPost, event?: Event): void {
    event?.stopPropagation();
    this.selectedPost.set(post);
    this.loadingDetail.set(true);

    this.contentApi.getById(post.contentItemId).subscribe({
      next: (detail) => {
        this.loadingDetail.set(false);
        this.selectedPost.update((current) =>
          current ? { ...current, content: detail.content, hashtags: detail.hashtags } : current,
        );
      },
      error: () => this.loadingDetail.set(false),
    });
  }

  closeModal(): void { this.selectedPost.set(null); }

  cancelPost(id: string): void {
    this.actionError.set(null);
    this.schedulingApi.cancel(id).subscribe({
      next: () => {
        this.posts.update(list => list.map(p => p.id === id ? { ...p, status: 'Cancelled' as PostStatus } : p));
        this.selectedPost.set(null);
      },
      error: (error: unknown) => {
        this.actionError.set(error instanceof ApiError ? error.message : 'تعذر إلغاء الجدولة.');
      },
    });
  }

  publishNow(id: string): void {
    this.actionError.set(null);
    this.schedulingApi.publishNow(id).subscribe({
      next: (result) => {
        this.posts.update(list => list.map(p => p.id === id ? { ...p, status: result.status } : p));
        this.selectedPost.set(null);
      },
      error: (error: unknown) => {
        this.actionError.set(error instanceof ApiError ? error.message : 'تعذر نشر المنشور الآن.');
      },
    });
  }
}
