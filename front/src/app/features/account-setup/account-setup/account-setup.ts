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
import { OtpVerification } from '../otp-verification/otp-verification';

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
    OtpVerification,
  ],
  templateUrl: './account-setup.html',
  styleUrl: './account-setup.css',
})
export class AccountSetup {
  private readonly router = inject(Router);
  private readonly storageKey = 'rawaj.account-setup';

  protected readonly step = signal(0);
  protected readonly data = signal<AccountSetupData>(this.loadData());

  protected readonly steps = computed<string[]>(() =>
    this.data().accountType === 'agency' ? AGENCY_STEPS : BUSINESS_STEPS
  );
  protected readonly totalSteps = computed(() => this.steps().length);
  protected readonly isOtpStep = computed(() => this.step() > 0 && this.step() === this.totalSteps() + 1);

  constructor() {
    effect(() => this.saveData(this.data()));
  }

  protected updateData(partial: Partial<AccountSetupData>): void {
    this.data.update(d => ({ ...d, ...partial }));
  }

  protected next(): void {
    this.step.update(s => s + 1);
  }

  protected back(): void {
    this.step.update(s => Math.max(0, s - 1));
  }

  protected selectType(type: 'agency' | 'business'): void {
    this.updateData({ accountType: type });
    this.next();
  }

  protected finishOtp(): void {
    localStorage.removeItem(this.storageKey);
    this.router.navigate(['/dashboard']);
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
