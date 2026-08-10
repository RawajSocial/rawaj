import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { SubscriptionService } from '../../../../services/subscription.service';
import { CoinPricingService } from '../../../../services/coin-pricing.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../../services/confirm-dialog.service';
import { CelebrationModalService } from '../../../../services/celebration-modal.service';
import {
  AddOnType,
  BILLING_TRANSACTION_TYPE_LABELS,
  BillingTransactionSummary,
  CreateCheckoutSessionResponse,
  SubscriptionPlanSummary,
} from '../../../../model/billing.model';
import { ApiResponse } from '../../../../model/auth.model';

@Component({
  selector: 'app-billing-page',
  imports: [PageHeader, FormsModule, DatePipe],
  templateUrl: './billing-page.html',
  styleUrls: ['../../dashboard-shared.css', './billing-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingPage {
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  protected readonly subscriptionService = inject(SubscriptionService);
  private readonly coinPricingService = inject(CoinPricingService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly celebrationModalService = inject(CelebrationModalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly transactionTypeLabels = BILLING_TRANSACTION_TYPE_LABELS;

  protected readonly tenant = this.tenantService.tenant;
  protected readonly coinBalance = this.tenantService.coinBalance;
  protected readonly plans = this.subscriptionService.plans;
  protected readonly subscription = this.subscriptionService.subscription;
  protected readonly aiCreditsUsage = this.subscriptionService.aiCreditsUsage;
  protected readonly pricing = this.coinPricingService.pricing;

  protected readonly currentPlan = computed(() =>
    this.plans().find(p => p.subscriptionPlanId === this.subscription()?.subscriptionPlanId),
  );

  // ── Plan picker ──
  protected readonly planPickerOpen = signal(false);
  protected readonly selectedPlanId = signal<string | null>(null);
  protected readonly agencySize = signal('');
  protected readonly servicesOfferedInput = signal('');

  protected readonly selectedPlan = computed(() => this.plans().find(p => p.subscriptionPlanId === this.selectedPlanId()));
  protected readonly needsAgencyInfo = computed(
    () => this.tenant()?.tenantType === 'Business' && (this.selectedPlan()?.cost ?? 0) > 0,
  );

  protected readonly agencySizeOptions = ['1-5', '6-15', '16-50', '50+'];

  // ── History ──
  protected readonly history = signal<BillingTransactionSummary[]>([]);
  protected readonly historyLoading = signal(false);

  // ── Buy coins ──
  protected readonly customCoins = signal<number | null>(null);

  /** True while any purchase/plan-change/cancel request is in flight — every buy/confirm button
   *  binds [disabled] to this, so a double-click can't fire the request twice. The backend also
   *  collapses accidental duplicate Stripe Checkout Session creations via an idempotency key, but
   *  disabling here is what stops a second request from firing in the first place. */
  protected readonly checkoutPending = signal(false);

  constructor() {
    this.seo.setPageSeo({
      title: 'الفوترة والاشتراك | رواج',
      description: 'تابع باقتك واستهلاكك وفواتيرك السابقة، واشترِ كوينز أو غيّر باقتك في أي وقت.',
      keywords: 'رواج, الفوترة, الاشتراك, الباقات, الفواتير, شراء كوينز',
      path: '/dashboard/billing',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    this.subscriptionService.refreshPlans().subscribe();
    this.subscriptionService.refreshSubscription().subscribe();
    this.subscriptionService.refreshAiCreditsUsage().subscribe();
    this.coinPricingService.ensureLoaded();

    // A redirect back from Stripe Checkout — clear the query params immediately (so a later
    // refresh doesn't re-trigger this) and, on success, confirm the payment landed.
    const checkoutStatus = this.route.snapshot.queryParamMap.get('checkout');
    const checkoutSessionId = this.route.snapshot.queryParamMap.get('session_id');
    if (checkoutStatus) {
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
    }

    this.loadHistory(() => {
      if (checkoutStatus === 'success') this.confirmCheckoutReturn(checkoutSessionId, this.history().length);
    });
  }

  private loadHistory(onLoaded?: () => void): void {
    this.historyLoading.set(true);
    this.subscriptionService.getBillingHistory(1, 20).subscribe({
      next: res => {
        this.historyLoading.set(false);
        if (res.data) this.history.set(res.data.items);
        onLoaded?.();
      },
      error: () => this.historyLoading.set(false),
    });
  }

  /** First tries the "verify on return" fallback (works with no Stripe webhook configured at
   *  all — e.g. local development without `stripe listen` running); if that isn't confirmed yet
   *  (or there's no session id to check), falls back to polling billing history for the real
   *  webhook to land, so either setup works. */
  private confirmCheckoutReturn(sessionId: string | null, previousHistoryCount: number): void {
    if (!sessionId) {
      this.pollForPaymentConfirmation(previousHistoryCount);
      return;
    }

    this.subscriptionService.verifyCheckoutSession(sessionId).subscribe({
      next: res => {
        if (res.status === 'success' && res.data) {
          this.onPaymentConfirmed();
        } else {
          this.pollForPaymentConfirmation(previousHistoryCount);
        }
      },
      error: () => this.pollForPaymentConfirmation(previousHistoryCount),
    });
  }

  /** Stripe's webhook usually lands within seconds, but the browser's return to this page proves
   *  nothing on its own — so this polls billing history for a new row rather than assuming success.
   *  On timeout, the charge did still succeed from Stripe's perspective even if our webhook is
   *  momentarily slow, so the message is reassuring rather than an error. */
  private pollForPaymentConfirmation(previousHistoryCount: number, attemptsLeft = 10): void {
    this.subscriptionService.getBillingHistory(1, 20).subscribe(res => {
      if (res.data) this.history.set(res.data.items);
      const landed = (res.data?.items.length ?? 0) > previousHistoryCount;

      if (landed) {
        this.onPaymentConfirmed();
        return;
      }

      if (attemptsLeft <= 1) {
        this.errorModalService.show(
          'تم استلام الدفع بنجاح، وسيتم تحديث حسابك خلال لحظات. حدّث الصفحة إذا لم يظهر التحديث.',
          { variant: 'success' },
        );
        return;
      }

      setTimeout(() => this.pollForPaymentConfirmation(previousHistoryCount, attemptsLeft - 1), 1500);
    });
  }

  private onPaymentConfirmed(): void {
    this.tenantService.refresh().subscribe();
    this.subscriptionService.refreshSubscription().subscribe();
    this.coinPricingService.refresh().subscribe();
    this.loadHistory();
    this.celebrationModalService.show('تم تأكيد الدفع بنجاح!', 'مبروك!');
  }

  protected aiCreditsPercent(): number {
    const usage = this.aiCreditsUsage();
    if (!usage || usage.maxCreditsMonthly === 0) return 0;
    return Math.min(100, Math.round((usage.usedThisMonth / usage.maxCreditsMonthly) * 100));
  }

  protected openPlanPicker(): void {
    this.selectedPlanId.set(null);
    this.agencySize.set('');
    this.servicesOfferedInput.set('');
    this.planPickerOpen.set(true);
  }

  protected selectPlan(plan: SubscriptionPlanSummary): void {
    this.selectedPlanId.set(plan.subscriptionPlanId);
  }

  protected async confirmPlanChange(): Promise<void> {
    if (this.checkoutPending()) return;
    const plan = this.selectedPlan();
    if (!plan) return;

    if (this.needsAgencyInfo() && !this.agencySize()) {
      this.errorModalService.show('اختر حجم الوكالة أولًا.');
      return;
    }

    this.checkoutPending.set(true);
    this.subscriptionService
      .changePlan({
        subscriptionPlanId: plan.subscriptionPlanId,
        agencySize: this.needsAgencyInfo() ? this.agencySize() : undefined,
        servicesOffered: this.needsAgencyInfo()
          ? this.servicesOfferedInput().split(',').map(s => s.trim()).filter(Boolean)
          : undefined,
      })
      .subscribe({
        next: res => {
          if (res.status === 'success' && res.data?.checkoutUrl) {
            // Paid plan — nothing has actually changed yet, leave the SPA for Stripe's hosted page.
            // checkoutPending stays true; the page is navigating away regardless.
            this.subscriptionService.redirectToCheckout(res.data.checkoutUrl);
            return;
          }

          this.checkoutPending.set(false);
          this.planPickerOpen.set(false);
          if (res.status === 'success' && res.data) {
            this.tenantService.refresh().subscribe();
            this.coinPricingService.refresh().subscribe();
            this.loadHistory();
            const grantNote = res.data.coinsGranted > 0
              ? `\nحصلت أيضًا على ${res.data.coinsGranted.toLocaleString('ar-EG')} كوين!`
              : '';
            this.celebrationModalService.show(`تم الاشتراك في باقة ${plan.name} بنجاح!${grantNote}`, 'مبروك!');
          } else {
            this.errorModalService.show(res.message ?? 'تعذّر تغيير الباقة.');
          }
        },
        error: err => {
          this.checkoutPending.set(false);
          this.planPickerOpen.set(false);
          this.errorModalService.show(err?.error?.message ?? 'تعذّر تغيير الباقة.');
        },
      });
  }

  protected async cancelSubscription(): Promise<void> {
    if (this.checkoutPending()) return;
    const freePlan = this.plans().find(p => p.cost === 0);
    if (!freePlan) return;

    const confirmed = await this.confirmDialogService.confirm(
      'سيتم إلغاء اشتراكك الحالي والعودة إلى الباقة المجانية، مع تقليل الحد الأقصى للعلامات التجارية وأعضاء الفريق. هل تريد المتابعة؟',
      { title: 'إلغاء الاشتراك', variant: 'danger', confirmLabel: 'نعم، إلغاء الاشتراك' },
    );
    if (!confirmed) return;

    this.checkoutPending.set(true);
    this.subscriptionService.changePlan({ subscriptionPlanId: freePlan.subscriptionPlanId }).subscribe({
      next: res => {
        this.checkoutPending.set(false);
        if (res.status === 'success') {
          this.tenantService.refresh().subscribe();
          this.coinPricingService.refresh().subscribe();
          this.loadHistory();
          this.errorModalService.show('تم إلغاء اشتراكك والعودة إلى الباقة المجانية.', { variant: 'success' });
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر إلغاء الاشتراك.');
        }
      },
      error: err => {
        this.checkoutPending.set(false);
        this.errorModalService.show(err?.error?.message ?? 'تعذّر إلغاء الاشتراك.');
      },
    });
  }

  protected buyPackage(coinPackageId: string): void {
    if (this.checkoutPending()) return;
    this.checkoutPending.set(true);
    this.subscriptionService.purchaseCoins({ coinPackageId }).subscribe({
      next: res => this.startCheckout(res),
      error: err => {
        this.checkoutPending.set(false);
        this.errorModalService.show(err?.error?.message ?? 'تعذّر بدء عملية الدفع.');
      },
    });
  }

  protected buyCustomCoins(): void {
    if (this.checkoutPending()) return;
    const amount = this.customCoins();
    if (!amount || amount <= 0) {
      this.errorModalService.show('أدخل عدد كوينز صحيحًا أولًا.');
      return;
    }

    this.checkoutPending.set(true);
    this.subscriptionService.purchaseCoins({ customCoins: amount }).subscribe({
      next: res => {
        this.customCoins.set(null);
        this.startCheckout(res);
      },
      error: err => {
        this.checkoutPending.set(false);
        this.errorModalService.show(err?.error?.message ?? 'تعذّر بدء عملية الدفع.');
      },
    });
  }

  protected buyAddOn(type: AddOnType): void {
    if (this.checkoutPending()) return;
    this.checkoutPending.set(true);
    this.subscriptionService.purchaseAddOn(type).subscribe({
      next: res => this.startCheckout(res),
      error: err => {
        this.checkoutPending.set(false);
        this.errorModalService.show(err?.error?.message ?? 'تعذّر بدء عملية الدفع.');
      },
    });
  }

  /** Both purchase-coins and purchase-add-on always return a checkout URL — there's no free tier
   *  for either, unlike plan changes. checkoutPending is left true on the redirect branch since the
   *  page is navigating away regardless. */
  private startCheckout(res: ApiResponse<CreateCheckoutSessionResponse>): void {
    if (res.status === 'success' && res.data?.checkoutUrl) {
      this.subscriptionService.redirectToCheckout(res.data.checkoutUrl);
    } else {
      this.checkoutPending.set(false);
      this.errorModalService.show(res.message ?? 'تعذّر بدء عملية الدفع.');
    }
  }
}
