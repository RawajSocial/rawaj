import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import { SubscriptionService } from '../../../services/subscription.service';

const PLAN_DESCRIPTIONS: Record<string, string> = {
  Free: 'للأفراد وأصحاب الأعمال الذين يديرون علامة تجارية واحدة بأنفسهم.',
  Plus: 'لفرق التسويق الصغيرة التي تدير عدة علامات تجارية بمرونة أكبر.',
  Pro: 'للوكالات المتنامية التي تحتاج فريقًا أكبر وخصمًا أعلى على استهلاك الكوينز.',
  Ultra: 'للوكالات الكبيرة التي تدير عشرات العلامات التجارية بفريق موسّع وأقصى توفير.',
};

/** Landing page's compact pricing teaser — real plan tiers/prices, no annual toggle (only monthly
 *  billing exists), links out to the full `/pricing` explainer page for coin packages/add-ons/AI
 *  price list rather than crowding the landing page with them. */
@Component({
  selector: 'app-pricing',
  imports: [GsapRevealDirective, RouterLink],
  templateUrl: './pricing.html',
  styleUrl: './pricing.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pricing {
  private readonly subscriptionService = inject(SubscriptionService);
  protected readonly plans = this.subscriptionService.plans;

  protected readonly plansWithDescriptions = computed(() =>
    this.plans().map(p => ({ ...p, description: PLAN_DESCRIPTIONS[p.name] ?? '' })),
  );

  constructor() {
    this.subscriptionService.refreshPlans().subscribe();
  }
}
