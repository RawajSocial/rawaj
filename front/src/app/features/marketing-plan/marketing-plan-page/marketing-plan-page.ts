import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { MediaService } from '../../../services/media.service';
import { GenType } from '../../../model/generated-item.model';
import { MpEmptyState } from '../mp-empty-state/mp-empty-state';
import { MpGenerating } from '../mp-generating/mp-generating';
import { MpPlansList } from '../mp-plans-list/mp-plans-list';
import { MpPlanDetail } from '../mp-plan-detail/mp-plan-detail';
import { BrandLock } from '../../../shared/components/brand-lock/brand-lock';
import { SeoService } from '../../../services/seo.service';
import { BrandContextService } from '../../../services/brand-context.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { CampaignService } from '../../../services/campaign.service';

// ──── Loading stages ────────────────────────────────────────────────────────
export interface Stage {
  id: number;
  label: string;
  icon: string;
  duration: number;
}

const STAGES: Stage[] = [
  { id: 0, label: 'تحليل بيانات نشاطك التجاري',          icon: 'fa-solid fa-magnifying-glass-chart', duration: 1300 },
  { id: 1, label: 'بناء ملف الجمهور المستهدف',            icon: 'fa-solid fa-users',                  duration: 1500 },
  { id: 2, label: 'إنشاء جدول المحتوى الشهري',            icon: 'fa-solid fa-calendar-days',          duration: 1800 },
  { id: 3, label: 'توليد الإعلانات والمواد الترويجية',    icon: 'fa-solid fa-wand-magic-sparkles',    duration: 2000 },
  { id: 4, label: 'حساب الميزانية وتوزيع الإنفاق',        icon: 'fa-solid fa-chart-pie',              duration: 1400 },
  { id: 5, label: 'تجميع الخطة التسويقية الشاملة',        icon: 'fa-solid fa-file-contract',          duration: 900  },
];

// ──── Platform / content maps ───────────────────────────────────────────────
const SLUG_TO_AR: Record<string, string> = {
  instagram: 'إنستغرام', facebook: 'فيسبوك', tiktok: 'تيك توك',
  snapchat: 'سناب شات', twitter: 'تويتر / X', youtube: 'يوتيوب',
  linkedin: 'لينكدإن',  whatsapp: 'واتساب',
};

const METRICS_AR: Record<string, string> = {
  followers: 'متابعون أكثر', visits: 'زيارات الموقع', sales: 'مبيعات أكثر',
  leads: 'عملاء محتملون', downloads: 'تحميلات أكثر', enquiries: 'استفسارات أكثر',
  awareness: 'وعي بالعلامة',
};

const PLATFORM_META: Record<string, { icon: string; color: string }> = {
  'إنستغرام':   { icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)' },
  'فيسبوك':     { icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)' },
  'تيك توك':    { icon: 'fa-brands fa-tiktok',     color: 'var(--color-tiktok)'    },
  'سناب شات':   { icon: 'fa-brands fa-snapchat',   color: 'var(--color-snapchat)' },
  'تويتر / X':  { icon: 'fa-brands fa-x-twitter',  color: 'var(--color-x)' },
  'يوتيوب':     { icon: 'fa-brands fa-youtube',    color: 'var(--color-youtube)' },
  'لينكدإن':    { icon: 'fa-brands fa-linkedin',   color: 'var(--color-linkedin)' },
  'واتساب':     { icon: 'fa-brands fa-whatsapp',   color: 'var(--color-whatsapp)' },
};

const CONTENT_TYPES = [
  { type: 'ريلز',     icon: 'fa-solid fa-film'        },
  { type: 'صورة',     icon: 'fa-solid fa-image'       },
  { type: 'ستوري',    icon: 'fa-solid fa-circle-dot'  },
  { type: 'فيديو',    icon: 'fa-solid fa-play'        },
  { type: 'كاروسيل', icon: 'fa-solid fa-layer-group'  },
  { type: 'بوست',     icon: 'fa-solid fa-align-right' },
];

const POST_TITLES = [
  'قصة العلامة التجارية', 'تعريف بالمنتج الجديد', 'عرض خاص للعملاء',
  'كيف نصنع منتجاتنا', '5 أسباب لاختيارنا', 'شهادة عميل',
  'استعراض المنتج', 'خلف الكواليس', 'نصائح للمستخدمين',
  'تحدي العلامة التجارية', 'سؤال وجواب مباشر', 'مقارنة المنتجات',
  'شكراً لعملائنا', 'عروض نهاية الموسم', 'لقطات من الفريق', 'منتج الأسبوع',
];

const POST_CAPTIONS = [
  'اكتشف عالماً من الأناقة والتميز مع منتجاتنا الحصرية ✨ كل تفصيل صُمِّم بعناية لأجلك.',
  'منتجنا الجديد هنا! 🚀 جاهز لتغيير تجربتك اليومية إلى الأفضل — جرّبه الآن.',
  '⚡ عرض لا يُفوَّت لفترة محدودة فقط — احصل عليه الآن قبل نفاد الكمية!',
  'نحن لا نبيع منتجاً، نبيع تجربة 🌟 اكتشف الفرق بنفسك واخبرنا.',
  '5 أسباب تجعلنا الخيار الأول لعملائنا 💡 اقرأ وانضم إلى عائلتنا.',
  '⭐ شهادات حقيقية من عملاء حقيقيين. تجربتهم هي أفضل دليل لنا.',
  'كيف يُصنع منتجنا؟ 🏭 جولة حصرية خلف الكواليس تنتظرك.',
  '💎 نصائح ذهبية من خبرائنا لتستفيد أكثر من منتجاتنا يومياً.',
  'تحدِّيك اليوم: جرّب منتجنا وأخبرنا برأيك! 📣 شاركنا تجربتك.',
  '❓ أسئلة وأجوبة مباشرة مع فريقنا. سؤالك يهمنا ونرد في أقل من ساعة!',
  'قارن وحكم بنفسك 🔍 نثق بجودتنا ونثق برأيك.',
  '🙏 شكراً لكل عميل وثق بنا. أنتم الدافع الذي يجعلنا نتطور يومياً.',
  '🔥 عروض نهاية الموسم وصلت! أسعار لن تتكرر — استغل الفرصة.',
  'تعرّف على فريقنا 👥 الأشخاص الذين يعملون بشغف من أجلك كل يوم.',
  '⏳ منتج الأسبوع بخصم استثنائي! أسرع قبل النفاد.',
  'رحلتنا بدأت بحلم بسيط 💫 تقديم الأفضل لك. واليوم نحن هنا.',
];

const POST_HASHTAGS: string[][] = [
  ['#منتجاتنا', '#تسوق_الآن', '#جودة_عالية'],
  ['#جديد', '#اطلاق', '#منتج_جديد'],
  ['#عروض', '#خصومات', '#لا_تفوّت'],
  ['#تجربة_مميزة', '#علامتنا', '#اختر_الأفضل'],
  ['#أسباب_للثقة', '#خبرة', '#احترافية'],
  ['#آراء_عملاء', '#ثقة', '#رضا_العملاء'],
  ['#خلف_الكواليس', '#صناعة', '#حرفية'],
  ['#نصائح', '#خبراء', '#تعلم_معنا'],
  ['#تحدي', '#شاركنا', '#مجتمعنا'],
  ['#أسئلة_وأجوبة', '#تواصل', '#نهتم_برأيك'],
  ['#مقارنة', '#جودة', '#اختبر_الفرق'],
  ['#شكراً', '#عملاؤنا', '#عائلتنا'],
  ['#تخفيضات', '#موسم', '#عروض_خاصة'],
  ['#فريقنا', '#شغف', '#وراء_الكواليس'],
  ['#منتج_الأسبوع', '#عرض_محدود', '#أسرع'],
  ['#قصتنا', '#رؤية', '#نمو'],
];

const MOCK_LIKES    = [348, 1240,  892, 2100,  567,  430, 1890,  730];
const MOCK_COMMENTS = [ 42,  138,   91,  204,   55,   38,  167,   83];
const MOCK_SHARES   = [127,  390,  210,  580,  142,   98,  430,  195];

const CAL_DAYS = ['السبت', 'الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة'];

// ──── Exported interfaces ────────────────────────────────────────────────────
export interface CalendarPost {
  id: string;
  monthIdx: number;
  weekRow: number;
  dayCol: number;
  day: string;
  platform: string;
  platformIcon: string;
  platformColor: string;
  type: string;
  typeIcon: string;
  title: string;
  caption: string;
  hashtags: string[];
  linkedMediaId?: string;
  likes: number;
  comments: number;
  shares: number;
}

export interface KpiRow {
  label: string;
  value: string;
  unit: string;
  icon: string;
  color: string;
}

export interface PlanData {
  brandName: string;
  sector: string;
  timeframe: string;
  goals: string[];
  platforms: string[];
  budgetFrom: number;
  budgetTo: number;
  ageFrom: number;
  ageTo: number;
  gender: string;
  interests: string[];
  productDesc: string;
  fbConn?: { connected: boolean; accountName?: string };
  igConn?: { connected: boolean; accountName?: string };
  kpis: KpiRow[];
  mediaItems: import('../../../model/generated-item.model').GeneratedItem[];
}

export interface EditedPlan {
  brand: string;
  goals: string[];
  budgetFrom: number;
  budgetTo: number;
}

export interface SavedPlan {
  id: string;
  createdAt: number;
  name: string;
  /** The real campaign's BriefJson (the onboarding wizard's collected answers), parsed — this
   *  page's calendar/budget/KPI preview is a deterministic function of these real, persisted
   *  answers, same as the plan-approval step's supplementary preview during the wizard itself. */
  data: OnboardingSnap;
  /** Brand/campaign this plan is associated with — every plan now maps 1:1 to a real
   *  MarketingCampaign row (see CampaignService.getCampaign), so this is always set. */
  brandProfileId?: string;
  campaignId?: string;
}

interface OnboardingSnap {
  // Brand
  brandName?: string;
  sector?: string;
  tagline?: string;
  productDesc?: string;
  uniqueValue?: string;
  // Campaign
  campaignType?: string;
  campaignDuration?: string;
  campaignOutcome?: string;
  // Audience
  gender?: 'female' | 'male' | 'all';
  ageRanges?: string[];
  audiencePlatforms?: string[];
  interests?: string[];
  // Strategy
  platformRanking?: string[];
  successMetrics?: string[];
  monthlyBudget?: string;
  positioningVs?: string;
  // Social
  socialConnections?: {
    facebook?: { connected: boolean; accountName?: string };
    instagram?: { connected: boolean; accountName?: string };
  };
  // Legacy fields (kept for backward compat)
  timeframe?: string;
  platforms?: string[];
  goals?: string[];
  budgetFrom?: number;
  budgetTo?: number;
  ageFrom?: number;
  ageTo?: number;
}

// ──── Component ─────────────────────────────────────────────────────────────
@Component({
  selector: 'app-marketing-plan-page',
  standalone: true,
  imports: [MpEmptyState, MpGenerating, MpPlansList, MpPlanDetail, BrandLock],
  templateUrl: './marketing-plan-page.html',
  styleUrl: './marketing-plan-page.css',
})
export class MarketingPlanPage {
  private readonly router     = inject(Router);
  private readonly media      = inject(MediaService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly seo        = inject(SeoService);
  private readonly brandContextService = inject(BrandContextService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly campaignService = inject(CampaignService);

  readonly stages = STAGES;
  readonly brandProfileCount = this.tenantService.brandProfileCount;

  // ── Phase ──
  readonly phase = signal<'empty' | 'generating' | 'plans' | 'detail'>('generating');

  // ── Generating ──
  readonly completedCount = signal(0);
  readonly progressPct    = computed(() => Math.round((this.completedCount() / STAGES.length) * 100));
  readonly streamedItems  = signal<CalendarPost[]>([]);

  // ── Plans ──
  readonly savedPlans   = signal<SavedPlan[]>([]);
  readonly activePlanId = signal<string | null>(null);

  /** The plans list respects the header's global brand/campaign filter. Plans that predate
   *  brand/campaign association (no `brandProfileId` set) stay visible under any selection —
   *  see the note on `SavedPlan`. "All Campaigns" aggregates every campaign of the brand. */
  readonly visiblePlans = computed(() => {
    const brandId = this.brandContextService.selectedBrandProfileId();
    const campaignId = this.brandContextService.selectedCampaignId();
    return this.savedPlans().filter(p => {
      const brandMatch = !p.brandProfileId || !brandId || p.brandProfileId === brandId;
      const campaignMatch = campaignId === 'all' || !p.campaignId || p.campaignId === campaignId;
      return brandMatch && campaignMatch;
    });
  });

  // ── Detail data ──
  private readonly snap      = signal<OnboardingSnap>({});
  private readonly calMonthCount = signal(1);
  readonly calPosts          = signal<CalendarPost[]>([]);

  readonly plan = computed<PlanData>(() => {
    const d = this.snap();
    const platforms  = this.resolvePlatforms(d);
    const { from: budgetFrom, to: budgetTo } = this.parseBudget(d);
    const { from: ageFrom,   to: ageTo }     = this.parseAge(d);
    const goals = this.resolveGoals(d);
    const duration = d.campaignDuration ?? d.timeframe ?? '3 أشهر';
    return {
      brandName:   d.brandName   ?? 'علامتك التجارية',
      sector:      d.sector      ?? '',
      timeframe:   duration,
      goals,
      platforms,
      budgetFrom,
      budgetTo,
      ageFrom,
      ageTo,
      gender:      d.gender      ?? 'all',
      interests:   d.interests   ?? [],
      productDesc: d.productDesc ?? '',
      fbConn:      d.socialConnections?.facebook,
      igConn:      d.socialConnections?.instagram,
      kpis:        this.buildKpis(budgetFrom, budgetTo),
      mediaItems:  this.media.items(),
    };
  });

  // ──── Constructor ──────────────────────────────────────────────────────────
  constructor() {
    this.seo.setPageSeo({
      title: 'الخطة التسويقية | رواج',
      description: 'خطتك التسويقية الكاملة مبنية على بيانات نشاطك، جاهزة للتنفيذ.',
      keywords: 'رواج, خطة تسويقية, تحليل الجمهور, توزيع الميزانية',
      path: '/dashboard/marketing-plan',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    this.phase.set('empty');
    // Refetch rather than trust whatever UserLayout already loaded — guarantees this page always
    // reflects the latest campaign list regardless of navigation timing.
    this.campaignService.refresh().subscribe(() => this.loadPlansFromCampaigns());
  }

  /** Every "plan" here is a real campaign that has a BriefJson — fetches each campaign's full
   *  detail (the list endpoint doesn't carry BriefJson) to know which ones have one. Replaces the
   *  old `localStorage['rawaj.plans']` store: the onboarding wizard now persists BriefJson on the
   *  real campaign row instead of writing to localStorage. */
  private loadPlansFromCampaigns(): void {
    const campaigns = this.campaignService.campaigns();
    if (campaigns.length === 0) {
      this.phase.set('empty');
      return;
    }

    forkJoin(
      campaigns.map(c =>
        this.campaignService.getCampaign(c.id).pipe(
          map(res => (res.data?.briefJson ? { campaign: c, briefJson: res.data.briefJson } : null)),
          catchError(() => of(null)),
        ),
      ),
    ).subscribe(results => {
      const plans: SavedPlan[] = results
        .filter((r): r is { campaign: typeof campaigns[number]; briefJson: string } => r !== null)
        .map(({ campaign, briefJson }) => {
          let data: OnboardingSnap = {};
          try { data = JSON.parse(briefJson) as OnboardingSnap; } catch { /* leave empty */ }
          return {
            id: campaign.id,
            createdAt: Date.parse(campaign.createdAt) || Date.now(),
            name: campaign.name,
            data,
            brandProfileId: campaign.brandProfileId,
            campaignId: campaign.id,
          };
        })
        .sort((a, b) => b.createdAt - a.createdAt);

      this.savedPlans.set(plans);
      this.phase.set(plans.length === 0 ? 'empty' : 'plans');
    });
  }

  // ── Nav ──
  goToOnboarding(): void {
    if (!this.requireBrandProfile()) return;
    void this.router.navigate(['/on-boarding'], { queryParams: { fresh: 1 } });
  }

  backToPlans(): void { this.phase.set('plans'); }

  openPlan(id: string): void {
    const p = this.savedPlans().find(sp => sp.id === id);
    if (!p) return;
    this.activePlanId.set(id);
    const months = this.parseMonths(p.data);
    this.snap.set(p.data);
    this.calMonthCount.set(months);
    this.calPosts.set(this.buildFlatCalendar(p.data, months));
    this.phase.set('detail');
  }

  // ── Actions ──
  remake(): void {
    if (!this.requireBrandProfile()) return;
    const snap   = this.snap();
    const months = this.parseMonths(snap);
    this.completedCount.set(0);
    this.streamedItems.set([]);
    const posts = this.buildFlatCalendar(snap, months);
    this.calPosts.set(posts);
    this.phase.set('generating');
    this.startGenerating(posts);
  }

  /** The real approval/content-generation flow already happened in the onboarding wizard
   *  (research → diagnosis → strategy → approve) — this just routes on to where the campaign's
   *  real content actually lives now. */
  approve(): void {
    const id = this.activePlanId();
    void this.router.navigate(id ? ['/dashboard/campaigns', id, 'content'] : ['/dashboard']);
  }

  /** Edits here only touch this page's local preview (brand name/goals/budget shown in the
   *  deterministic calendar) — they are not persisted. The real strategy edit is the "عدّل الخطة"
   *  free-text box during the wizard's approval step (RefineCampaignPlanCommand). */
  handleEditSave(e: EditedPlan): void {
    const updated: OnboardingSnap = {
      ...this.snap(),
      brandName:  e.brand,
      goals:      e.goals,
      budgetFrom: e.budgetFrom,
      budgetTo:   e.budgetTo,
    };
    this.snap.set(updated);
    const id = this.activePlanId();
    if (id) {
      this.savedPlans.update(plans => plans.map(p =>
        p.id === id ? { ...p, data: updated } : p
      ));
    }
  }

  private requireBrandProfile(): boolean {
    if (this.tenantService.brandProfileCount() > 0) return true;
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    void this.router.navigate(['/dashboard/brand-profiles/new']);
    return false;
  }

  // ──── Private helpers ──────────────────────────────────────────────────────
  private startGenerating(posts: CalendarPost[]): void {
    let delay = 0;
    STAGES.forEach((stage, idx) => {
      delay += stage.duration;
      const id = setTimeout(() => {
        this.completedCount.set(idx + 1);
        if (idx === STAGES.length - 1) {
          const finId = setTimeout(() => this.phase.set('detail'), 800);
          this.destroyRef.onDestroy(() => clearTimeout(finId));
        }
      }, delay);
      this.destroyRef.onDestroy(() => clearTimeout(id));
    });

    // Stream content cards one-by-one
    const toStream = posts.slice(0, 16);
    toStream.forEach((post, i) => {
      const id = setTimeout(() => {
        this.streamedItems.update(items => [...items, post]);
      }, 1400 + i * 560);
      this.destroyRef.onDestroy(() => clearTimeout(id));
    });
  }

  private parseMonths(d: OnboardingSnap): number {
    const tf = d.campaignDuration ?? d.timeframe ?? '3 أشهر';
    const m = tf.match(/(\d+)/);
    return Math.max(1, Math.min(m ? +m[0] : 3, 12));
  }

  private resolvePlatforms(d: OnboardingSnap): string[] {
    const ranked = (d.platformRanking ?? []).map(p => SLUG_TO_AR[p] ?? p);
    if (ranked.length) return ranked;
    const audience = (d.audiencePlatforms ?? []).map(p => SLUG_TO_AR[p] ?? p);
    if (audience.length) return audience;
    return d.platforms?.length ? d.platforms : ['إنستغرام', 'فيسبوك'];
  }

  private resolveGoals(d: OnboardingSnap): string[] {
    if (d.goals?.length) return d.goals;
    return (d.successMetrics ?? []).map(m => METRICS_AR[m] ?? m);
  }

  private parseBudget(d: OnboardingSnap): { from: number; to: number } {
    if (d.budgetFrom && d.budgetTo) return { from: d.budgetFrom, to: d.budgetTo };
    const budget = d.monthlyBudget ?? '';
    const nums = budget.match(/[\d,]+/g)?.map(n => parseInt(n.replace(/,/g, ''), 10)) ?? [];
    if (nums.length >= 2) return { from: nums[0], to: nums[1] };
    if (nums.length === 1) return { from: Math.round(nums[0] * 0.6), to: nums[0] };
    return { from: 3000, to: 8000 };
  }

  private parseAge(d: OnboardingSnap): { from: number; to: number } {
    if (d.ageFrom && d.ageTo) return { from: d.ageFrom, to: d.ageTo };
    const ranges = d.ageRanges ?? [];
    if (!ranges.length) return { from: 18, to: 35 };
    const nums = ranges.flatMap(r => r.match(/\d+/g)?.map(Number) ?? []);
    if (!nums.length) return { from: 18, to: 35 };
    return { from: Math.min(...nums), to: Math.max(...nums) };
  }

  private buildFlatCalendar(d: OnboardingSnap, months: number): CalendarPost[] {
    const platforms = this.resolvePlatforms(d);
    const mediaIds  = this.media.items().map(i => i.id);
    const posts: CalendarPost[] = [];
    let idx = 0;

    for (let m = 0; m < months; m++) {
      const counts = [4, 5, 4, 4];
      for (let w = 0; w < 4; w++) {
        const count = counts[w];
        for (let i = 0; i < count; i++) {
          const platform = platforms[idx % platforms.length];
          const pm = PLATFORM_META[platform] ?? { icon: 'fa-solid fa-hashtag', color: '#6b7280' };
          const ct = CONTENT_TYPES[idx % CONTENT_TYPES.length];
          const dayCol = Math.min(Math.floor((i / count) * 6), 5);
          posts.push({
            id:            `post-${m}-${w}-${i}`,
            monthIdx:      m,
            weekRow:       w,
            dayCol,
            day:           CAL_DAYS[dayCol],
            platform,
            platformIcon:  pm.icon,
            platformColor: pm.color,
            type:          ct.type,
            typeIcon:      ct.icon,
            title:         POST_TITLES[idx % POST_TITLES.length],
            caption:       POST_CAPTIONS[idx % POST_CAPTIONS.length],
            hashtags:      POST_HASHTAGS[idx % POST_HASHTAGS.length],
            linkedMediaId: mediaIds.length ? mediaIds[idx % mediaIds.length] : undefined,
            likes:         MOCK_LIKES[idx % MOCK_LIKES.length],
            comments:      MOCK_COMMENTS[idx % MOCK_COMMENTS.length],
            shares:        MOCK_SHARES[idx % MOCK_SHARES.length],
          });
          idx++;
        }
      }
    }
    return posts;
  }

  private buildKpis(from: number, to: number): KpiRow[] {
    const budget = (from + to) / 2;
    const fmt = (n: number): string => {
      if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'م';
      if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'ك';
      return Math.round(n).toLocaleString('ar-SA');
    };
    return [
      { label: 'الوصول الشهري التقديري',  value: fmt(budget * 12),   unit: 'شخص',      icon: 'fa-solid fa-users',         color: '#7C3AED' },
      { label: 'الانطباعات الشهرية',       value: fmt(budget * 42),   unit: 'ظهور',     icon: 'fa-solid fa-eye',           color: '#3b82f6' },
      { label: 'معدل التفاعل المتوقع',     value: '2.8',              unit: '%',        icon: 'fa-solid fa-heart',         color: '#e91e8c' },
      { label: 'النقرات الشهرية',          value: fmt(budget * 0.7),  unit: 'نقرة',     icon: 'fa-solid fa-arrow-pointer', color: '#f97316' },
      { label: 'تكلفة الألف ظهور (CPM)',   value: '9.5',              unit: 'ريال',     icon: 'fa-solid fa-chart-bar',     color: '#22c55e' },
      { label: 'الميزانية اليومية',        value: fmt(budget / 30),   unit: 'ريال/يوم', icon: 'fa-solid fa-sack-dollar',   color: '#eab308' },
    ];
  }
}
