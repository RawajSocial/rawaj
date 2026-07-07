import { Component, HostListener, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ScheduledPost, PostStatus } from '../../../model/scheduled-post.model';
import { CampaignPlatform } from '../../../model/campaign.model';
import { PostModal } from '../post-modal/post-modal';

export type ViewMode = 'month' | 'week' | 'day' | 'list';

const PLATFORM_CFG: Record<CampaignPlatform, { icon: string; color: string; label: string }> = {
  instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
  facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#222',    label: 'تيك توك'  },
  youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
  x:         { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: '#FDD835', label: 'سناب شات' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
};

const MOCK_CAMPAIGNS = [
  { id: '1', name: 'حملة رمضان الكريم' },
  { id: '2', name: 'إطلاق منتج العيد' },
  { id: '3', name: 'حملة الصيف' },
];

const INITIAL_POSTS: ScheduledPost[] = [
  // ─ June 1 ─
  { id: 'p1',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'اكتشف تشكيلتنا الجديدة بمناسبة رمضان الكريم 🌙\nخصومات تصل إلى ٥٠٪ على جميع المنتجات المختارة', mediaType: 'image',    scheduledAt: '2026-06-01T10:00:00', status: 'published', hashtags: ['#رمضان_كريم', '#عروض', '#تخفيضات'],    estimatedReach: 45000 },
  { id: 'p2',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'عروض رمضان لا تفوتك! تسوق الآن واستمتع بأفضل الأسعار',                                                   mediaType: 'image',    scheduledAt: '2026-06-01T14:00:00', status: 'published', hashtags: ['#رمضان', '#عروض_مميزة'],                estimatedReach: 32000 },
  // ─ June 3 ─
  { id: 'p3b', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'الصيف قادم! جهّز نفسك مع أفضل منتجاتنا الصيفية ☀️',                                                       mediaType: 'story',    scheduledAt: '2026-06-03T08:30:00', status: 'published', hashtags: ['#صيف', '#منتجات_صيفية'] },
  // ─ June 4 (today) ─
  { id: 'p3',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'منتجنا الجديد وصل أخيراً! كن أول من يجربه ويشارك تجربته معنا 🎉',                                          mediaType: 'carousel', scheduledAt: '2026-06-04T09:00:00', status: 'scheduled', hashtags: ['#منتج_جديد', '#عيد', '#إطلاق'],          estimatedReach: 58000 },
  { id: 'p4',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'tiktok',    content: 'شاهد كيف يعمل منتجنا الجديد في هذا الفيديو المميز 🎬 #ترند',                                               mediaType: 'video',    scheduledAt: '2026-06-04T12:00:00', status: 'scheduled', hashtags: ['#تيك_توك', '#منتج', '#ترند'],               estimatedReach: 120000 },
  { id: 'p5',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'لا تفوت عرض اليوم! خصم خاص لمتابعينا الكرام ٣٠٪ على الطلبات فوق ٢٠٠ ريال',                               mediaType: 'image',    scheduledAt: '2026-06-04T18:00:00', status: 'scheduled', hashtags: ['#خصم', '#عرض_خاص'],                        estimatedReach: 28000 },
  // ─ June 5 ─
  { id: 'p6',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'استمر الزخم! شارك مع أصدقائك وحصل على خصم إضافي',                                                          mediaType: 'story',    scheduledAt: '2026-06-05T11:00:00', status: 'scheduled', hashtags: ['#شارك_واربح'],                             estimatedReach: 15000 },
  { id: 'p7',  campaignId: '3', campaignName: 'حملة الصيف',        platform: 'youtube',   content: 'فيديو جديد: نصائح الصيف الذهبية من خبرائنا لتكون مستعداً لهذا الصيف 🎥',                                   mediaType: 'video',    scheduledAt: '2026-06-05T16:00:00', status: 'scheduled', hashtags: ['#نصائح_صيفية', '#يوتيوب'],                 estimatedReach: 22000 },
  // ─ June 8 ─
  { id: 'p8',  campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'snapchat',  content: 'قصة سناب حصرية: كواليس إطلاق منتجنا الجديد 👻',                                                            mediaType: 'story',    scheduledAt: '2026-06-08T10:00:00', status: 'scheduled', hashtags: ['#سناب_شات', '#حصري'],                      estimatedReach: 18000 },
  { id: 'p8b', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'facebook',  content: 'أبرز منتجات الصيف لهذا العام — قائمة مختارة بعناية',                                                       mediaType: 'image',    scheduledAt: '2026-06-08T15:00:00', status: 'scheduled',                                                    estimatedReach: 24000 },
  // ─ June 10 ─
  { id: 'p9',  campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'instagram', content: 'منتصف الرحلة ونحن لا زلنا نقدم أفضل العروض! لا تفوتها',                                                   mediaType: 'carousel', scheduledAt: '2026-06-10T09:00:00', status: 'scheduled',                                                    estimatedReach: 41000 },
  { id: 'p10', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'facebook',  content: 'تحضيرات الصيف: قائمة مشترياتك الأساسية التي لا يمكنك الاستغناء عنها',                                     mediaType: 'image',    scheduledAt: '2026-06-10T15:00:00', status: 'scheduled',                                                    estimatedReach: 19000 },
  // ─ June 12 ─
  { id: 'p11', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'آراء عملائنا تتحدث عن نفسها! تجربة حقيقية من عملاء حقيقيين',                                              mediaType: 'image',    scheduledAt: '2026-06-12T11:00:00', status: 'scheduled', hashtags: ['#آراء_العملاء', '#تجربة_حقيقية'],           estimatedReach: 36000 },
  { id: 'p11b',campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'x',         content: 'أسعار لم نقدمها من قبل! تفقد عروضنا الحصرية الآن 🔥',                                                      mediaType: 'image',    scheduledAt: '2026-06-12T14:00:00', status: 'scheduled', hashtags: ['#عروض_حصرية'],                             estimatedReach: 12000 },
  // ─ June 15 ─
  { id: 'p12', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'tiktok',    content: 'تحدي الصيف ابدأ معنا الآن! 🌊☀️ شارك مقطعك وستظهر على صفحتنا',                                            mediaType: 'video',    scheduledAt: '2026-06-15T14:00:00', status: 'scheduled', hashtags: ['#تحدي_الصيف', '#تيك_توك'],                 estimatedReach: 95000 },
  { id: 'p13', campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'linkedin',  content: 'شراكات تجارية جديدة وتوسع مستمر في السوق الخليجي — نتطلع للتعاون معكم',                                   mediaType: 'image',    scheduledAt: '2026-06-15T09:00:00', status: 'scheduled',                                                    estimatedReach: 8500  },
  // ─ June 18 ─
  { id: 'p18', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'instagram', content: 'أسبوع التميز! خصومات إضافية تصل لـ ٤٠٪ على المجموعة الكاملة',                                             mediaType: 'carousel', scheduledAt: '2026-06-18T10:00:00', status: 'scheduled',                                                    estimatedReach: 52000 },
  // ─ June 20 ─
  { id: 'p14', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'نصف الطريق للصيف الكامل! وعروضنا لا تتوقف، ابقوا معنا',                                                   mediaType: 'carousel', scheduledAt: '2026-06-20T10:00:00', status: 'scheduled',                                                    estimatedReach: 44000 },
  { id: 'p20b',campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'الأسبوع الأخير! سارع قبل انتهاء العروض التي قد لا تتكرر',                                                 mediaType: 'image',    scheduledAt: '2026-06-20T17:00:00', status: 'scheduled',                                                    estimatedReach: 31000 },
  // ─ June 22 ─
  { id: 'p22', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'youtube',   content: 'حلقة جديدة: كيف تستعد لعطلة الصيف بأقل تكلفة وأعلى جودة',                                                 mediaType: 'video',    scheduledAt: '2026-06-22T14:00:00', status: 'scheduled',                                                    estimatedReach: 27000 },
  // ─ June 25 ─
  { id: 'p15', campaignId: '2', campaignName: 'إطلاق منتج العيد',  platform: 'youtube',   content: 'الحلقة الأخيرة: مراجعة شاملة لمنتجنا مع الخبراء 🎥',                                                      mediaType: 'video',    scheduledAt: '2026-06-25T16:00:00', status: 'scheduled',                                                    estimatedReach: 35000 },
  { id: 'p16', campaignId: '1', campaignName: 'حملة رمضان الكريم', platform: 'facebook',  content: 'آخر أيام العروض الاستثنائية! سارع قبل انتهاء المخزون للأبد',                                              mediaType: 'image',    scheduledAt: '2026-06-25T18:00:00', status: 'scheduled', hashtags: ['#آخر_فرصة', '#عروض'],                       estimatedReach: 29000 },
  // ─ June 28 ─
  { id: 'p28', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'الختام الكبير لحملة الصيف! شكراً لكل من شارك وتفاعل معنا 🙏',                                             mediaType: 'image',    scheduledAt: '2026-06-28T10:00:00', status: 'scheduled',                                                    estimatedReach: 48000 },
  // ─ June 30 ─
  { id: 'p17', campaignId: '3', campaignName: 'حملة الصيف',        platform: 'instagram', content: 'نهاية شهر وبداية فصل جديد! شكراً لدعمكم المستمر، والمزيد قادم 🙏',                                        mediaType: 'image',    scheduledAt: '2026-06-30T10:00:00', status: 'scheduled', hashtags: ['#شكراً', '#معاً_للأمام'],                  estimatedReach: 51000 },
];

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [RouterLink, PostModal],
  templateUrl: './calendar-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './calendar-page.css'],
})
export class CalendarPage {
  readonly viewMode       = signal<ViewMode>('month');
  readonly currentDate    = signal(new Date(2026, 5, 4)); // June 4 2026
  readonly campaignFilter = signal<string>('all');
  readonly campaignOpen   = signal(false);
  readonly selectedPost   = signal<ScheduledPost | null>(null);
  readonly posts          = signal<ScheduledPost[]>(INITIAL_POSTS);

  readonly platformCfg  = PLATFORM_CFG;
  readonly campaigns     = MOCK_CAMPAIGNS;
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

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  goToDay(date: Date): void {
    this.currentDate.set(new Date(date));
    this.viewMode.set('day');
  }

  get campaignLabel(): string {
    return MOCK_CAMPAIGNS.find(c => c.id === this.campaignFilter())?.name ?? 'جميع الحملات';
  }

  setCampaign(id: string): void {
    this.campaignFilter.set(id);
    this.campaignOpen.set(false);
  }

  // ── Post modal ──────────────────────────────────────────────────────────

  openPost(post: ScheduledPost, event?: Event): void {
    event?.stopPropagation();
    this.selectedPost.set(post);
  }

  closeModal(): void { this.selectedPost.set(null); }

  savePost(updated: ScheduledPost): void {
    this.posts.update(list => list.map(p => p.id === updated.id ? updated : p));
    this.selectedPost.set(null);
  }

  deletePost(id: string): void {
    this.posts.update(list => list.filter(p => p.id !== id));
    this.selectedPost.set(null);
  }
}
