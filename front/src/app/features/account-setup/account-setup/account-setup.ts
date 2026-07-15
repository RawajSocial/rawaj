import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { SetupStepper } from '../setup-stepper/setup-stepper';
import { SetupTypeSelector } from '../setup-type-selector/setup-type-selector';
import { AgencyBasicInfo } from '../agency/agency-basic-info/agency-basic-info';
import { AgencySizeClients } from '../agency/agency-size-clients/agency-size-clients';
import { AgencyServices } from '../agency/agency-services/agency-services';
import { AgencyLocation } from '../agency/agency-location/agency-location';
import { AgencySocialLinks } from '../agency/agency-social-links/agency-social-links';
import { BusinessBasicInfo } from '../business/business-basic-info/business-basic-info';
import { BusinessDetails } from '../business/business-details/business-details';
import { BusinessLocation } from '../business/business-location/business-location';
import { BusinessSocialLinks } from '../business/business-social-links/business-social-links';
import { TenantsApiService } from '../../../core/api/tenants-api.service';
import { BrandProfilesApiService } from '../../../core/api/brand-profiles-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { TenantType } from '../../../core/models';

export type AccountSetupData = {
  email?: string;
  accountType?: 'agency' | 'business';
  agencyName?: string;
  agencyPhone?: string;
  agencyCountryCode?: string;
  agencySize?: string;
  activeClients?: string;
  primaryServices?: string[];
  businessName?: string;
  businessPhone?: string;
  businessCountryCode?: string;
  industry?: string;
  businessSize?: string;
  hearAboutUs?: string;
  country?: string;
  city?: string;
  website?: string;
  facebook?: string;
  instagram?: string;
  youtube?: string;
  tiktok?: string;
  linkedin?: string;
  x?: string;
  snapchat?: string;
};

const AGENCY_STEPS = ['المعلومات الأساسية', 'الحجم والعملاء', 'الخدمات', 'الموقع', 'روابط التواصل'];
const BUSINESS_STEPS = ['المعلومات الأساسية', 'تفاصيل النشاط', 'الموقع', 'روابط التواصل'];

@Component({
  selector: 'app-account-setup',
  imports: [
    RouterLink,
    SetupStepper,
    SetupTypeSelector,
    AgencyBasicInfo,
    AgencySizeClients,
    AgencyServices,
    AgencyLocation,
    AgencySocialLinks,
    BusinessBasicInfo,
    BusinessDetails,
    BusinessLocation,
    BusinessSocialLinks,
  ],
  templateUrl: './account-setup.html',
  styleUrl: './account-setup.css',
})
export class AccountSetup {
  private readonly router = inject(Router);
  private readonly tenantsApi = inject(TenantsApiService);
  private readonly brandProfilesApi = inject(BrandProfilesApiService);
  private readonly tenantService = inject(TenantService);
  private readonly storageKey = 'rawaj.account-setup';

  protected readonly step = signal(0);
  protected readonly data = signal<AccountSetupData>(this.loadData());
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly steps = computed<string[]>(() =>
    this.data().accountType === 'agency' ? AGENCY_STEPS : BUSINESS_STEPS
  );
  protected readonly totalSteps = computed(() => this.steps().length);

  constructor() {
    effect(() => this.saveData(this.data()));
  }

  protected updateData(partial: Partial<AccountSetupData>): void {
    this.data.update(d => ({ ...d, ...partial }));
  }

  protected next(): void {
    if (this.step() >= this.totalSteps()) {
      this.completeSetup();
      return;
    }
    this.step.update(s => s + 1);
  }

  protected back(): void {
    this.step.update(s => Math.max(0, s - 1));
  }

  protected selectType(type: 'agency' | 'business'): void {
    this.updateData({ accountType: type });
    this.next();
  }

  private completeSetup(): void {
    this.submitError.set(null);
    this.submitting.set(true);

    const data = this.data();
    const isAgency = data.accountType === 'agency';
    const workspaceName = (isAgency ? data.agencyName : data.businessName)?.trim() || 'مساحة عمل رواج';
    const tenantType: TenantType = isAgency ? 'Agency' : 'Business';

    this.tenantsApi.create({ name: workspaceName, subdomain: this.slugify(workspaceName), tenantType }).subscribe({
      next: () => {
        this.brandProfilesApi
          .create({
            name: workspaceName,
            industry: data.industry ?? null,
            websiteUrl: data.website ?? null,
          })
          .subscribe({
            next: () => {
              this.tenantService.loadContext().subscribe({
                next: () => {
                  this.submitting.set(false);
                  localStorage.removeItem(this.storageKey);
                  this.router.navigate(['/on-boarding']);
                },
                error: () => {
                  // Tenant + brand were created successfully; a failure caching them locally
                  // shouldn't block the user, the dashboard/onboarding will fetch fresh anyway.
                  this.submitting.set(false);
                  localStorage.removeItem(this.storageKey);
                  this.router.navigate(['/on-boarding']);
                },
              });
            },
            error: (error: unknown) => this.handleSubmitError(error),
          });
      },
      error: (error: unknown) => this.handleSubmitError(error),
    });
  }

  private handleSubmitError(error: unknown): void {
    this.submitting.set(false);
    this.submitError.set(error instanceof ApiError ? error.message : 'تعذر إنشاء مساحة العمل، حاول مرة أخرى.');
  }

  private slugify(name: string): string {
    const base = name
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9؀-ۿ]+/g, '-')
      .replace(/^-+|-+$/g, '');
    const suffix = Math.random().toString(36).slice(2, 6);
    return `${base || 'workspace'}-${suffix}`;
  }

  private loadData(): AccountSetupData {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return {};
    try { return JSON.parse(raw) as AccountSetupData; } catch { return {}; }
  }

  private saveData(data: AccountSetupData): void {
    localStorage.setItem(this.storageKey, JSON.stringify(data));
  }
}
