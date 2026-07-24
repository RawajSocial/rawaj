import { Component, DestroyRef, inject, input, OnInit, output, signal, computed } from '@angular/core';
import { CampaignService } from '../../../services/campaign.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import {
  BusinessDiagnosis, CampaignStrategy, CompetitorResearch,
} from '../../../model/campaign.model';

// ──── Platform & content metadata ─────────────────────────────────────────
const PLATFORM_META: Record<string, { icon: string; color: string; lightColor: string; label: string }> = {
  instagram: { icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)', lightColor: '#fce7f3',  label: 'Instagram'   },
  facebook:  { icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)',  lightColor: '#dbeafe',  label: 'Facebook'    },
  tiktok:    { icon: 'fa-brands fa-tiktok',     color: 'var(--color-tiktok)',    lightColor: '#f3f4f6',  label: 'TikTok'      },
  snapchat:  { icon: 'fa-brands fa-snapchat',   color: 'var(--color-snapchat)',  lightColor: '#fef9c3',  label: 'Snapchat'    },
  twitter:   { icon: 'fa-brands fa-x-twitter',  color: 'var(--color-x)',         lightColor: '#f3f4f6',  label: 'X / Twitter' },
  youtube:   { icon: 'fa-brands fa-youtube',    color: 'var(--color-youtube)',   lightColor: '#fee2e2',  label: 'YouTube'     },
  linkedin:  { icon: 'fa-brands fa-linkedin',   color: 'var(--color-linkedin)',  lightColor: '#dbeafe',  label: 'LinkedIn'    },
  whatsapp:  { icon: 'fa-brands fa-whatsapp',   color: 'var(--color-whatsapp)',  lightColor: '#dcfce7',  label: 'WhatsApp'    },
};

const CONTENT_TYPES = {
  reel:     { label: 'ريل',      icon: 'fa-solid fa-film',        color: '#8b5cf6' },
  story:    { label: 'قصة',      icon: 'fa-solid fa-circle-play', color: '#06b6d4' },
  post:     { label: 'بوست',     icon: 'fa-solid fa-image',       color: '#3b82f6' },
  carousel: { label: 'كاروسيل',  icon: 'fa-solid fa-images',      color: '#f59e0b' },
  video:    { label: 'فيديو',    icon: 'fa-solid fa-video',       color: '#ef4444' },
  poll:     { label: 'استفتاء',  icon: 'fa-solid fa-square-poll-vertical', color: '#10b981' },
} as const;

const SUCCESS_METRICS_MAP: Record<string, { label: string; target: string; icon: string }> = {
  followers: { label: 'متابعون أكثر',    target: '+2,500 متابع/شهر',   icon: 'fa-solid fa-user-plus' },
  visits:    { label: 'زيارات الموقع',    target: '10,000+ زيارة/شهر',  icon: 'fa-solid fa-globe' },
  sales:     { label: 'مبيعات أكثر',      target: '+30% في المبيعات',   icon: 'fa-solid fa-arrow-trend-up' },
  leads:     { label: 'عملاء محتملون',    target: '200+ عميل/شهر',      icon: 'fa-solid fa-bullseye' },
  downloads: { label: 'تحميلات أكثر',     target: '5,000+ تحميل/شهر',   icon: 'fa-solid fa-download' },
  enquiries: { label: 'استفسارات أكثر',   target: '150+ استفسار/شهر',   icon: 'fa-solid fa-envelope' },
  awareness: { label: 'وعي بالعلامة',     target: '75,000+ وصول شهري',  icon: 'fa-solid fa-bullhorn' },
};

@Component({
  selector: 'app-onboarding-plan-approval',
  imports: [],
  templateUrl: './onboarding-plan-approval.html',
  styleUrl: './onboarding-plan-approval.css',
})
export class OnboardingPlanApproval implements OnInit {
  readonly data       = input<ApprovalOnboardingData | null>(null);
  readonly campaignId = input<string | null>(null);
  readonly approve    = output<void>();
  readonly back       = output<void>();

  private readonly destroyRef = inject(DestroyRef);
  private readonly campaignService = inject(CampaignService);
  private readonly errorModalService = inject(ErrorModalService);

  protected readonly phase       = signal<'loading' | 'plan'>('loading');
  protected readonly loadingStep = signal(0);

  protected readonly loadingMessages = [
    'البحث عن المنافسين في نفس نشاطك...',
    'تحليل ما فهمناه عن نشاطك...',
    'بناء استراتيجية الحملة الكاملة...',
  ];

  // ── Real AI pipeline state (research → diagnosis → strategy) ──
  protected readonly competitorResearch = signal<CompetitorResearch | null>(null);
  protected readonly diagnosis          = signal<BusinessDiagnosis | null>(null);
  protected readonly strategy           = signal<CampaignStrategy | null>(null);
  protected readonly pipelineError      = signal<string | null>(null);

  protected readonly refineFeedback = signal('');
  protected readonly refining       = signal(false);
  protected readonly approving      = signal(false);
  protected readonly approved       = signal(false);

  protected readonly plan = computed<GeneratedPlan>(() => {
    const d        = this.data() ?? {};
    const platforms = this.resolvePlatforms(d);
    const months    = this.parseMonths(d.campaignDuration ?? '');
    const budget    = this.parseBudget(d.monthlyBudget ?? '');
    const postsPerMonth = this.calcPostsPerMonth(platforms);

    return {
      brandName:        d.brandName ?? 'علامتك التجارية',
      sector:           d.sector ?? '',
      goal:             this.resolveGoal(d),
      duration:         d.campaignDuration ?? '3 أشهر',
      months,
      totalPosts:       postsPerMonth * months,
      postsPerWeek:     Math.max(3, Math.round(postsPerMonth / 4.3)),
      weeklySchedule:   this.buildWeeklySchedule(platforms),
      platformStrategy: this.buildPlatformStrategy(platforms, d),
      budgetBreakdown:  this.buildBudget(budget),
      kpis:             this.buildKPIs(d.successMetrics ?? []),
      insights:         this.buildInsights(d, platforms),
    };
  });

  ngOnInit(): void {
    const campaignId = this.campaignId();
    if (!campaignId) {
      // No real campaign to run the pipeline against (shouldn't happen from the wizard, but keep
      // the page usable rather than stuck on a loader forever).
      this.pipelineError.set('تعذّر العثور على الحملة. عد للخطوة السابقة وحاول مرة أخرى.');
      this.phase.set('plan');
      return;
    }
    this.runPipeline(campaignId);
  }

  /** Runs the real research → diagnosis → strategy pipeline once, in order, against the campaign
   *  the wizard just created. Competitor research is best-effort and never blocks progress — a
   *  Tavily failure just means the "unavailable" note renders instead of competitor cards. */
  private runPipeline(campaignId: string): void {
    this.loadingStep.set(0);
    this.campaignService.researchCompetitors(campaignId).subscribe({
      next: res => {
        if (res.data) {
          this.competitorResearch.set(this.parseJson<CompetitorResearch>(res.data.competitorResearchJson));
        }
        this.runDiagnosis(campaignId);
      },
      error: () => this.runDiagnosis(campaignId), // best-effort — proceed regardless
    });
  }

  private runDiagnosis(campaignId: string): void {
    this.loadingStep.set(1);
    this.campaignService.diagnoseBusiness(campaignId).subscribe({
      next: res => {
        if (res.data) this.diagnosis.set(this.parseJson<BusinessDiagnosis>(res.data.diagnosisJson));
        this.runStrategy(campaignId);
      },
      error: err => {
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر إعداد تحليل النشاط.'));
        this.runStrategy(campaignId); // still attempt the strategy — diagnosis is advisory input to it
      },
    });
  }

  private runStrategy(campaignId: string): void {
    this.loadingStep.set(2);
    this.campaignService.generatePlan(campaignId).subscribe({
      next: res => {
        if (res.data) this.strategy.set(this.parseJson<CampaignStrategy>(res.data.aiPlanJson));
        this.phase.set('plan');
      },
      error: err => {
        this.pipelineError.set(extractApiErrorMessage(err, 'تعذّر توليد الاستراتيجية. يمكنك المحاولة مجدداً أو المتابعة بالخطة الأولية.'));
        this.phase.set('plan');
      },
    });
  }

  private parseJson<T>(raw: string | null | undefined): T | null {
    if (!raw) return null;
    try { return JSON.parse(raw) as T; } catch { return null; }
  }

  /** "عدّل الخطة" — free-text refinement of the just-generated strategy (AI Reasoning Conversation). */
  protected submitRefine(): void {
    const campaignId = this.campaignId();
    const feedback = this.refineFeedback().trim();
    if (!campaignId || !feedback || this.refining()) return;

    this.refining.set(true);
    this.campaignService.refinePlan(campaignId, feedback).subscribe({
      next: res => {
        this.refining.set(false);
        if (res.data) {
          this.strategy.set(this.parseJson<CampaignStrategy>(res.data.aiPlanJson));
          this.refineFeedback.set('');
        }
      },
      error: err => {
        this.refining.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تعديل الخطة.'), { variant: 'error' });
      },
    });
  }

  protected updateRefineFeedback(value: string): void {
    this.refineFeedback.set(value);
  }

  // ──── Helpers ─────────────────────────────────────────────────────────────

  private resolvePlatforms(d: ApprovalOnboardingData): string[] {
    const ranked   = d.platformRanking  ?? [];
    const audience = d.audiencePlatforms ?? [];
    const merged   = [...new Set([...ranked, ...audience])];
    return merged.length ? merged.slice(0, 4) : ['instagram', 'facebook'];
  }

  private parseMonths(tf: string): number {
    const m = tf.match(/(\d+)/);
    return Math.max(1, Math.min(m ? +m[0] : 3, 12));
  }

  private parseBudget(budget: string): BudgetRange {
    if (!budget || budget.includes('لا ميزانية')) return { avg: 0, label: budget };
    const nums = budget.match(/[\d,]+/g)?.map(n => parseInt(n.replace(/,/g, ''), 10)) ?? [];
    if (nums.length >= 2) return { avg: Math.round((nums[0] + nums[1]) / 2), label: budget };
    if (nums.length === 1) return { avg: nums[0], label: budget };
    return { avg: 5000, label: budget };
  }

  private calcPostsPerMonth(platforms: string[]): number {
    const perPlatform: Record<string, number> = {
      instagram: 12, facebook: 10, tiktok: 14, snapchat: 16,
      twitter: 18, youtube: 4, linkedin: 8, whatsapp: 0,
    };
    return Math.max(8, platforms.reduce((sum, p) => sum + (perPlatform[p] ?? 8), 0));
  }

  private resolveGoal(d: ApprovalOnboardingData): string {
    if (d.campaignOutcome) return d.campaignOutcome.slice(0, 120);
    const map: Record<string, string> = {
      brand_awareness: 'بناء وعي قوي وانتشار واسع للعلامة التجارية',
      sales:           'تعزيز المبيعات وتحقيق نمو إيرادي ملموس',
      leads:           'توليد عملاء محتملين ذوي جودة عالية',
      engagement:      'تعزيز تفاعل الجمهور وبناء مجتمع متفاعل',
      launch:          'إطلاق ناجح يحقق أقصى تأثير في وقت قصير',
    };
    return map[d.campaignType ?? ''] ?? 'بناء حضور رقمي متميز يحقق أهدافك التجارية';
  }

  private buildWeeklySchedule(platforms: string[]): DayPlan[] {
    const p  = (key: string): PlatformMeta => ({
      key,
      ...(PLATFORM_META[key] ?? { icon: 'fa-solid fa-hashtag', color: '#6b7280', lightColor: '#f3f4f6', label: key }),
    });
    const ct = (type: keyof typeof CONTENT_TYPES): ContentTypeMeta => CONTENT_TYPES[type];

    const primary   = platforms[0] ?? 'instagram';
    const secondary = platforms[1] ?? 'facebook';
    const tertiary  = platforms[2];

    return [
      {
        dayName: 'السبت', dayShort: 'سبت', isRest: false,
        slots: [{ platform: p(primary), contentType: ct('reel'), topic: 'محتوى ترويجي جذاب يبرز العلامة' }],
      },
      {
        dayName: 'الأحد', dayShort: 'أحد', isRest: false,
        slots: [
          { platform: p(secondary), contentType: ct('post'),     topic: 'تعليمي — قيمة للجمهور' },
          ...(tertiary ? [{ platform: p(tertiary), contentType: ct('video'), topic: 'خلف الكواليس' }] : []),
        ],
      },
      {
        dayName: 'الإثنين', dayShort: 'إثنين', isRest: false,
        slots: [{ platform: p(primary), contentType: ct('story'), topic: 'استفتاء / سؤال تفاعلي' }],
      },
      {
        dayName: 'الثلاثاء', dayShort: 'ثلاثاء', isRest: false,
        slots: [
          { platform: p(primary),   contentType: ct('carousel'), topic: 'كاروسيل ذو قيمة تعليمية' },
          { platform: p(secondary), contentType: ct('post'),     topic: 'مشاركة مجتمعية' },
        ],
      },
      {
        dayName: 'الأربعاء', dayShort: 'أربعاء', isRest: false,
        slots: [{ platform: p(primary), contentType: ct('story'), topic: 'عرض محدود المدة / حصري' }],
      },
      {
        dayName: 'الخميس', dayShort: 'خميس', isRest: false,
        slots: [
          { platform: p(primary), contentType: ct('reel'), topic: 'ريل أسبوعي — ترند أو نصيحة' },
          ...(tertiary ? [{ platform: p(tertiary), contentType: ct('post'), topic: 'محتوى مخصص للمنصة' }] : []),
        ],
      },
      { dayName: 'الجمعة', dayShort: 'جمعة', isRest: true, slots: [] },
    ];
  }

  private buildPlatformStrategy(platforms: string[], d: ApprovalOnboardingData): PlatformPlan[] {
    const postsMap: Record<string, number> = {
      instagram: 12, facebook: 10, tiktok: 14, snapchat: 16,
      twitter: 18, youtube: 4, linkedin: 8, whatsapp: 0,
    };
    const timesMap: Record<string, string> = {
      instagram: '6–9 م', facebook: '1–3 م', tiktok: '7–10 م',
      snapchat: '5–8 م', twitter: '12–2 م', youtube: '3–6 م',
      linkedin: '8–10 ص', whatsapp: 'مستمر',
    };
    const contentTypesMap: Record<string, string[]> = {
      instagram: ['ريلز', 'قصص', 'كاروسيل'],
      facebook:  ['بوستات', 'فيديو', 'إعلانات'],
      tiktok:    ['فيديو قصير', 'ترند', 'تحدي'],
      snapchat:  ['قصص يومية', 'سناب بلس'],
      twitter:   ['تغريدات', 'خيوط', 'استفتاء'],
      youtube:   ['فيديو طويل', 'شورتس'],
      linkedin:  ['مقالات', 'بوستات مهنية'],
      whatsapp:  ['حالة', 'إشعارات'],
    };

    return platforms.slice(0, 4)
      .filter(p => postsMap[p] > 0)
      .map((p, i) => ({
        key:           p,
        name:          PLATFORM_META[p]?.label ?? p,
        icon:          PLATFORM_META[p]?.icon ?? 'fa-solid fa-hashtag',
        color:         PLATFORM_META[p]?.color ?? '#6b7280',
        lightColor:    PLATFORM_META[p]?.lightColor ?? '#f3f4f6',
        postsPerMonth: postsMap[p] ?? 8,
        contentTypes:  contentTypesMap[p] ?? ['محتوى متنوع'],
        bestTime:      timesMap[p] ?? '6–9 م',
        isPrimary:     i === 0,
        reason:        this.platformReason(p, d),
      }));
  }

  private platformReason(platform: string, d: ApprovalOnboardingData): string {
    const sector = d.sector ?? 'نشاطك';
    const gLabel = d.gender === 'female' ? 'الجمهور النسائي' : d.gender === 'male' ? 'الجمهور الذكوري' : 'جمهورك المتنوع';
    const reasons: Record<string, string> = {
      instagram: `المنصة الأقوى بصرياً لعرض ${sector}، خاصةً مع ${gLabel} — الريلز والقصص تحقق أعلى وصول عضوي.`,
      facebook:  `تغطية واسعة وأدوات إعلانية متقدمة تناسب تعزيز المبيعات وبناء مجتمع حول علامتك في ${sector}.`,
      tiktok:    `منصة النمو الأسرع — مثالية لمحتوى خلف الكواليس والترند الأصيل، خاصةً للجمهور الشاب في ${sector}.`,
      snapchat:  `فعّالة جداً في السوق الخليجي للوصول المباشر لـ${gLabel} بقصص لحظية تعزز الثقة.`,
      twitter:   `مثالية للتفاعل اللحظي وبناء سمعة الخبرة في ${sector} عبر الحوار والتغريدات التثقيفية.`,
      youtube:   `محتوى طويل الأمد يبني مصداقية عميقة ويجذب عملاء مدروسين يبحثون عن ${sector}.`,
      linkedin:  `تواصل مهني مباشر مع أصحاب القرار والعملاء ذوي القيمة العالية في ${sector}.`,
      whatsapp:  `تواصل مباشر لتعزيز الولاء وإشعار قاعدة العملاء الحاليين.`,
    };
    return reasons[platform] ?? `منصة مناسبة لاستهداف جمهورك في ${sector}.`;
  }

  private buildBudget(budget: BudgetRange): BudgetLine[] {
    const avg = budget.avg;
    const hasBudget = avg > 0;

    return [
      {
        label:       'إنتاج المحتوى',
        percentage:  35,
        color:       '#8b5cf6',
        amount:      hasBudget ? `${Math.round(avg * 0.35).toLocaleString('ar-SA')} ر.س` : '35%',
        description: 'تصوير، مونتاج، تصميم جرافيك',
      },
      {
        label:       'الإعلانات المدفوعة',
        percentage:  40,
        color:       '#3b82f6',
        amount:      hasBudget ? `${Math.round(avg * 0.40).toLocaleString('ar-SA')} ر.س` : '40%',
        description: 'بوست مدفوع، إعلانات مستهدفة',
      },
      {
        label:       'التسويق عبر المؤثرين',
        percentage:  15,
        color:       '#ec4899',
        amount:      hasBudget ? `${Math.round(avg * 0.15).toLocaleString('ar-SA')} ر.س` : '15%',
        description: 'نانو ومايكرو مؤثرين متخصصين',
      },
      {
        label:       'أدوات وتحليلات',
        percentage:  10,
        color:       '#10b981',
        amount:      hasBudget ? `${Math.round(avg * 0.10).toLocaleString('ar-SA')} ر.س` : '10%',
        description: 'أدوات جدولة، تحليل، تقارير',
      },
    ];
  }

  private buildKPIs(metrics: string[]): KpiItem[] {
    const defaults = ['awareness', 'followers', 'sales'];
    const list = metrics.length ? metrics : defaults;
    return list.slice(0, 5).map(m => ({
      label:  SUCCESS_METRICS_MAP[m]?.label  ?? m,
      target: SUCCESS_METRICS_MAP[m]?.target ?? 'نمو ملحوظ',
      icon:   SUCCESS_METRICS_MAP[m]?.icon   ?? 'fa-solid fa-chart-line',
    }));
  }

  private buildInsights(d: ApprovalOnboardingData, platforms: string[]): InsightItem[] {
    const insights: InsightItem[] = [];

    if (d.sector) {
      insights.push({
        icon: 'fa-solid fa-lightbulb',
        text: `بناءً على تحليل قطاع "${d.sector}"، ركّزنا على المحتوى البصري الجذاب والقصص الأصيلة التي تتحدث بلغة جمهورك.`,
      });
    }

    if (d.positioningVs) {
      insights.push({
        icon: 'fa-solid fa-chess',
        text: `تموضعك التنافسي اقترح استراتيجية تمييز واضحة — المحتوى سيُبرز قيمتك الفريدة ليس مجرد الترويج.`,
      });
    }

    if (platforms.length) {
      const names = platforms.slice(0, 3).map(p => PLATFORM_META[p]?.label ?? p).join('، ');
      insights.push({
        icon: 'fa-solid fa-share-nodes',
        text: `اخترنا ${names} كمنصات أساسية بناءً على ترتيبك، تركيبة جمهورك، وأنماط المشاركة في سوقك.`,
      });
    }

    const gLabel = d.gender === 'female' ? 'الجمهور النسائي' : d.gender === 'male' ? 'الجمهور الذكوري' : 'كلا الجنسين';
    insights.push({
      icon: 'fa-solid fa-users',
      text: `نبرة المحتوى وأوقات النشر صُمِّمت خصيصاً لـ${gLabel} بحيث تحقق أعلى تفاعل في أوقات ذروة النشاط.`,
    });

    insights.push({
      icon: 'fa-solid fa-calendar-check',
      text: `جدول النشر الأسبوعي يوازن بين الاستمرارية والجودة — 6 أيام نشاط ويوم راحة لضمان استدامة الأداء طوال الحملة.`,
    });

    return insights.slice(0, 4);
  }

  onApprove(): void {
    const campaignId = this.campaignId();
    if (!campaignId || this.approving()) return;

    this.approving.set(true);
    this.campaignService.approvePlan(campaignId).subscribe({
      next: () => {
        this.approving.set(false);
        this.approved.set(true);
        this.approve.emit();
      },
      error: err => {
        this.approving.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر اعتماد الخطة.'), { variant: 'error' });
      },
    });
  }

  onBack(): void { this.back.emit(); }
}

// ──── Types ────────────────────────────────────────────────────────────────
export type ApprovalOnboardingData = {
  brandName?: string;
  sector?: string;
  campaignType?: string;
  campaignDuration?: string;
  campaignOutcome?: string;
  gender?: 'female' | 'male' | 'all';
  audiencePlatforms?: string[];
  successMetrics?: string[];
  positioningVs?: string;
  monthlyBudget?: string;
  platformRanking?: string[];
};

type BudgetRange = { avg: number; label: string };

type GeneratedPlan = {
  brandName: string;
  sector: string;
  goal: string;
  duration: string;
  months: number;
  totalPosts: number;
  postsPerWeek: number;
  weeklySchedule: DayPlan[];
  platformStrategy: PlatformPlan[];
  budgetBreakdown: BudgetLine[];
  kpis: KpiItem[];
  insights: InsightItem[];
};

type PlatformMeta = { key: string; icon: string; color: string; lightColor: string; label: string };
type ContentTypeMeta = { label: string; icon: string; color: string };

type DayPlan = {
  dayName: string;
  dayShort: string;
  isRest: boolean;
  slots: { platform: PlatformMeta; contentType: ContentTypeMeta; topic: string }[];
};

type PlatformPlan = {
  key: string;
  name: string;
  icon: string;
  color: string;
  lightColor: string;
  postsPerMonth: number;
  contentTypes: string[];
  bestTime: string;
  isPrimary: boolean;
  reason: string;
};

type BudgetLine = {
  label: string;
  percentage: number;
  color: string;
  amount: string;
  description: string;
};

type KpiItem = { label: string; target: string; icon: string };
type InsightItem = { icon: string; text: string };
