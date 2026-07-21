import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TenantsApiService } from '../../../core/api/tenants-api.service';
import { BrandProfilesApiService } from '../../../core/api/brand-profiles-api.service';
import { BillingApiService } from '../../../core/api/billing-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { SubscriptionPlanSummary, TenantType } from '../../../core/models';

const TENANT_TYPES: { value: TenantType; label: string; icon: string; hint: string }[] = [
  { value: 'Business',   label: 'نشاط تجاري',  icon: 'fa-store',       hint: 'أدير علامة تجارية واحدة بنفسي' },
  { value: 'Agency',     label: 'وكالة تسويق',  icon: 'fa-building',    hint: 'أدير حسابات عملاء متعددين' },
  { value: 'Freelancer', label: 'مستقل',        icon: 'fa-user-tie',    hint: 'أعمل بشكل مستقل مع عملائي' },
];

@Component({
  selector: 'app-account-setup',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './account-setup.html',
  styleUrls: ['../../dashboard/users/users-shared.css', './account-setup.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountSetup {
  private readonly router = inject(Router);
  private readonly tenantsApi = inject(TenantsApiService);
  private readonly brandProfilesApi = inject(BrandProfilesApiService);
  private readonly billingApi = inject(BillingApiService);
  private readonly tenantService = inject(TenantService);
  private readonly fb = inject(FormBuilder);

  protected readonly tenantTypes = TENANT_TYPES;

  protected readonly plans = signal<SubscriptionPlanSummary[]>([]);
  protected readonly loadingPlans = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly selectedPlanId = signal<string | null>(null);
  protected readonly selectedType = signal<TenantType>('Business');

  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    industry: [''],
    website: [''],
  });

  protected readonly selectedPlan = computed(() =>
    this.plans().find((p) => p.subscriptionPlanId === this.selectedPlanId()) ?? null,
  );

  constructor() {
    this.billingApi.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        const freePlan = plans.find((p) => p.name === 'Free') ?? plans[0] ?? null;
        this.selectedPlanId.set(freePlan?.subscriptionPlanId ?? null);
        this.loadingPlans.set(false);
      },
      error: (error: unknown) => {
        this.loadingPlans.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الباقات المتاحة.');
      },
    });
  }

  protected selectPlan(planId: string): void {
    this.selectedPlanId.set(planId);
  }

  protected selectType(type: TenantType): void {
    this.selectedType.set(type);
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, industry, website } = this.form.getRawValue();
    const tenantType = this.selectedType();
    const plan = this.selectedPlan();

    this.submitting.set(true);
    this.submitError.set(null);

    this.tenantsApi.create({ name: name.trim(), subdomain: this.slugify(name), tenantType }).subscribe({
      next: () => {
        // Free is already the default plan a new tenant gets - only call out to change it when
        // something else was picked, rather than making a redundant no-op request every time.
        const upgrade$ = plan && plan.name !== 'Free'
          ? this.billingApi.changePlan(plan.subscriptionPlanId)
          : null;

        const afterPlan = () => this.createBrandAndFinish(name.trim(), industry, website);

        if (upgrade$) {
          upgrade$.subscribe({ next: afterPlan, error: (error: unknown) => this.handleSubmitError(error) });
        } else {
          afterPlan();
        }
      },
      error: (error: unknown) => this.handleSubmitError(error),
    });
  }

  private createBrandAndFinish(name: string, industry: string, website: string): void {
    this.brandProfilesApi
      .create({ name, industry: industry.trim() || null, websiteUrl: website.trim() || null })
      .subscribe({
        next: () => {
          this.tenantService.loadContext().subscribe({
            next: () => this.finish(),
            // Tenant + brand were created successfully; a failure caching them locally shouldn't
            // block the user, the dashboard will fetch fresh context anyway.
            error: () => this.finish(),
          });
        },
        error: (error: unknown) => this.handleSubmitError(error),
      });
  }

  private finish(): void {
    this.submitting.set(false);
    void this.router.navigate(['/dashboard']);
  }

  private handleSubmitError(error: unknown): void {
    this.submitting.set(false);
    this.submitError.set(error instanceof ApiError ? error.message : 'تعذر إنشاء النشاط التجاري، حاول مرة أخرى.');
  }

  private slugify(name: string): string {
    const base = name
      .trim()
      .toLowerCase()
      // The backend requires an ASCII-only subdomain (^[a-z0-9-]+$) - Arabic names (the common
      // case here) fall through to just the random suffix below rather than being rejected.
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
    const suffix = Math.random().toString(36).slice(2, 6);
    return `${base || 'workspace'}-${suffix}`;
  }
}
