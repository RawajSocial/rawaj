import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { SubscriptionService } from '../../../../services/subscription.service';
import { CoinPricingService } from '../../../../services/coin-pricing.service';
import { ErrorModalService } from '../../../../services/error-modal.service';
import { ConfirmDialogService } from '../../../../services/confirm-dialog.service';
import { CelebrationModalService } from '../../../../services/celebration-modal.service';
import { FakePaymentModalService } from '../../../../services/fake-payment-modal.service';
import {
  AddOnType,
  BILLING_TRANSACTION_TYPE_LABELS,
  BillingTransactionSummary,
  PurchaseCoinsResponse,
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
  private readonly fakePaymentModalService = inject(FakePaymentModalService);

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
    this.loadHistory();
  }

  private loadHistory(): void {
    this.historyLoading.set(true);
    this.subscriptionService.getBillingHistory(1, 20).subscribe({
      next: res => {
        this.historyLoading.set(false);
        if (res.data) this.history.set(res.data.items);
      },
      error: () => this.historyLoading.set(false),
    });
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
    const plan = this.selectedPlan();
    if (!plan) return;

    if (this.needsAgencyInfo() && !this.agencySize()) {
      this.errorModalService.show('اختر حجم الوكالة أولًا.');
      return;
    }

    const priceLabel = plan.cost > 0 ? `$${plan.cost.toFixed(2)} / شهريًا` : 'مجانًا';
    const confirmed = await this.fakePaymentModalService.confirm({
      title: `الاشتراك في باقة ${plan.name}`,
      priceLabel,
    });
    if (!confirmed) return;

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
          this.planPickerOpen.set(false);
          this.errorModalService.show(err?.error?.message ?? 'تعذّر تغيير الباقة.');
        },
      });
  }

  protected async cancelSubscription(): Promise<void> {
    const freePlan = this.plans().find(p => p.cost === 0);
    if (!freePlan) return;

    const confirmed = await this.confirmDialogService.confirm(
      'سيتم إلغاء اشتراكك الحالي والعودة إلى الباقة المجانية، مع تقليل الحد الأقصى للعلامات التجارية وأعضاء الفريق. هل تريد المتابعة؟',
      { title: 'إلغاء الاشتراك', variant: 'danger', confirmLabel: 'نعم، إلغاء الاشتراك' },
    );
    if (!confirmed) return;

    this.subscriptionService.changePlan({ subscriptionPlanId: freePlan.subscriptionPlanId }).subscribe({
      next: res => {
        if (res.status === 'success') {
          this.tenantService.refresh().subscribe();
          this.coinPricingService.refresh().subscribe();
          this.loadHistory();
          this.errorModalService.show('تم إلغاء اشتراكك والعودة إلى الباقة المجانية.', { variant: 'success' });
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر إلغاء الاشتراك.');
        }
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر إلغاء الاشتراك.'),
    });
  }

  protected async buyPackage(coinPackageId: string, name: string, priceUsd: number): Promise<void> {
    const confirmed = await this.fakePaymentModalService.confirm({
      title: `شراء باقة ${name}`,
      priceLabel: `$${priceUsd.toFixed(2)}`,
      confirmLabel: 'تأكيد الشراء',
    });
    if (!confirmed) return;

    this.subscriptionService.purchaseCoins({ coinPackageId }).subscribe({
      next: res => this.onCoinsPurchased(res),
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر إتمام عملية الشراء.'),
    });
  }

  protected async buyCustomCoins(): Promise<void> {
    const amount = this.customCoins();
    if (!amount || amount <= 0) {
      this.errorModalService.show('أدخل عدد كوينز صحيحًا أولًا.');
      return;
    }
    const price = amount * (this.pricing()?.customCoinPricePerCoin ?? 0.01);
    const confirmed = await this.fakePaymentModalService.confirm({
      title: `شراء ${amount.toLocaleString('ar-EG')} كوين`,
      priceLabel: `$${price.toFixed(2)}`,
      confirmLabel: 'تأكيد الشراء',
    });
    if (!confirmed) return;

    this.subscriptionService.purchaseCoins({ customCoins: amount }).subscribe({
      next: res => {
        this.customCoins.set(null);
        this.onCoinsPurchased(res);
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر إتمام عملية الشراء.'),
    });
  }

  private onCoinsPurchased(res: ApiResponse<PurchaseCoinsResponse>): void {
    if (res.status === 'success' && res.data) {
      this.tenantService.refresh().subscribe();
      this.loadHistory();
      this.celebrationModalService.show(`تمت إضافة ${res.data.coinsGranted.toLocaleString('ar-EG')} كوين إلى رصيدك!`, 'مبروك!');
    } else {
      this.errorModalService.show(res.message ?? 'تعذّر إتمام عملية الشراء.');
    }
  }

  protected async buyAddOn(type: AddOnType, label: string, priceUsd: number): Promise<void> {
    const confirmed = await this.fakePaymentModalService.confirm({
      title: `شراء ${label}`,
      priceLabel: `$${priceUsd.toFixed(2)} / شهريًا`,
      confirmLabel: 'تأكيد الشراء',
    });
    if (!confirmed) return;

    this.subscriptionService.purchaseAddOn(type).subscribe({
      next: res => {
        if (res.status === 'success') {
          this.tenantService.refresh().subscribe();
          this.loadHistory();
          this.celebrationModalService.show(`تمت إضافة ${label} بنجاح!`, 'مبروك!');
        } else {
          this.errorModalService.show(res.message ?? 'تعذّر إتمام عملية الشراء.');
        }
      },
      error: err => this.errorModalService.show(err?.error?.message ?? 'تعذّر إتمام عملية الشراء.'),
    });
  }
}
