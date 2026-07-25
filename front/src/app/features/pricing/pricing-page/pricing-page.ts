import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../../services/seo.service';
import { SubscriptionService } from '../../../services/subscription.service';
import { AuthService } from '../../../core/auth/auth.service';
import { PublicCoinPricing } from '../../../model/billing.model';

const PLAN_DESCRIPTIONS: Record<string, string> = {
  Free: 'للأفراد وأصحاب الأعمال الذين يديرون علامة تجارية واحدة بأنفسهم — بداية مثالية بلا مخاطرة.',
  Plus: 'لفرق التسويق الصغيرة التي تدير عدة علامات تجارية وتحتاج مساعدين إضافيين.',
  Pro: 'للوكالات المتنامية التي تحتاج فريقًا أكبر وخصمًا أعلى على استهلاك الكوينز.',
  Ultra: 'للوكالات الكبيرة التي تدير عشرات العلامات التجارية بفريق موسّع وأقصى توفير ممكن.',
};

interface AiFeatureRow {
  label: string;
  cost: number;
  comingSoon?: boolean;
}

/** Public, logged-out-friendly page explaining every part of the pricing model in full — the four
 *  subscription tiers, coin packages, add-ons, and the AI feature price list. Reachable from the
 *  landing page's pricing teaser; lives outside the dashboard entirely (route `/pricing`, no guard). */
@Component({
  selector: 'app-pricing-page',
  imports: [RouterLink],
  templateUrl: './pricing-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './pricing-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricingPage {
  private readonly seo = inject(SeoService);
  private readonly subscriptionService = inject(SubscriptionService);
  protected readonly authService = inject(AuthService);

  protected readonly plans = this.subscriptionService.plans;
  protected readonly plansWithDescriptions = computed(() =>
    this.plans().map(p => ({ ...p, description: PLAN_DESCRIPTIONS[p.name] ?? '' })),
  );

  private readonly _publicPricing = signal<PublicCoinPricing | null>(null);
  protected readonly publicPricing = this._publicPricing.asReadonly();

  protected readonly aiFeatureRows = computed<AiFeatureRow[]>(() => {
    const p = this.publicPricing();
    if (!p) return [];
    return [
      { label: 'منشور تواصل اجتماعي واحد', cost: p.baseCosts.contentGeneration },
      { label: 'صورة واحدة بالذكاء الاصطناعي', cost: p.baseCosts.visualGeneration },
      { label: 'دفعة محتوى حملة كاملة', cost: p.baseCosts.campaignContentGeneration },
      { label: 'استراتيجية تسويقية كاملة', cost: p.baseCosts.marketingPlanGeneration },
      { label: 'جدولة منشور / قصة / ريلز / كاروسيل / تصميم ترويجي', cost: p.baseCosts.scheduling },
      { label: 'تشخيص العمل بالذكاء الاصطناعي', cost: p.baseCosts.businessDiagnosis },
      { label: 'تحليل المنافسين', cost: p.baseCosts.competitiveAnalysis },
      { label: 'محادثة استدلال ذكاء اصطناعي', cost: p.baseCosts.reasoningConversation },
    ];
  });

  constructor() {
    this.seo.setPageSeo({
      title: 'الأسعار والباقات | رواج',
      description: 'كل ما تحتاج معرفته عن باقات رواج، حزم الكوينز، الإضافات، وأسعار ميزات الذكاء الاصطناعي.',
      keywords: 'رواج, الأسعار, الباقات, الكوينز, الذكاء الاصطناعي',
      path: '/pricing',
      image: '/home-hero-light.png',
      type: 'website',
    });

    this.subscriptionService.refreshPlans().subscribe();
    this.subscriptionService.getPublicCoinPricing().subscribe(res => {
      if (res.data) this._publicPricing.set(res.data);
    });
  }

  protected planCtaNote(planCost: number): string {
    if (this.authService.isAuthenticated()) {
      return planCost > 0
        ? `سيتم تفعيل الباقة فورًا مقابل $${planCost.toFixed(2)} شهريًا (دفعة تجريبية).`
        : 'سيتم إلغاء اشتراكك الحالي والعودة إلى هذه الباقة فورًا.';
    }
    return 'أنشئ حسابًا مجانيًا أولًا، ثم فعّل هذه الباقة من صفحة الفوترة.';
  }

  protected planCtaTarget(): string {
    return this.authService.isAuthenticated() ? '/dashboard/billing' : '/sign-up';
  }
}
