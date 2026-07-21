import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { BillingApiService } from '../../../../core/api/billing-api.service';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { ApiError } from '../../../../core/api';
import { CurrentSubscription, SubscriptionPlanSummary } from '../../../../core/models';

@Component({
  selector: 'app-billing-page',
  imports: [PageHeader, DatePipe],
  templateUrl: './billing-page.html',
  styleUrls: ['../../dashboard-shared.css', './billing-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingPage {
  private readonly billingApi = inject(BillingApiService);
  private readonly tenantService = inject(TenantService);

  protected readonly canManageBilling = computed(() => this.tenantService.tenant()?.role === 'Owner');

  protected readonly currentSubscription = signal<CurrentSubscription | null>(null);
  protected readonly plans = signal<SubscriptionPlanSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly changingPlanId = signal<string | null>(null);
  protected readonly changeError = signal<string | null>(null);

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.billingApi.getMine().subscribe({
      next: (subscription) => this.currentSubscription.set(subscription),
      error: (error: unknown) => {
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل بيانات الاشتراك.');
      },
    });

    this.billingApi.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الباقات.');
      },
    });
  }

  protected isCurrentPlan(plan: SubscriptionPlanSummary): boolean {
    return this.currentSubscription()?.planName === plan.name;
  }

  protected changePlan(plan: SubscriptionPlanSummary): void {
    this.changingPlanId.set(plan.subscriptionPlanId);
    this.changeError.set(null);

    this.billingApi.changePlan(plan.subscriptionPlanId).subscribe({
      next: () => {
        this.changingPlanId.set(null);
        this.load();
      },
      error: (error: unknown) => {
        this.changingPlanId.set(null);
        this.changeError.set(error instanceof ApiError ? error.message : 'تعذر تغيير الباقة، حاول مرة أخرى.');
      },
    });
  }
}
