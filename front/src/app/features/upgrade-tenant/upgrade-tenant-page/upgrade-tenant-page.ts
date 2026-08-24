import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SetupStepper } from '../setup-stepper/setup-stepper';
import { AgencyBasicInfo } from '../agency/agency-basic-info/agency-basic-info';
import { AgencySizeClients } from '../agency/agency-size-clients/agency-size-clients';
import { AgencyServices } from '../agency/agency-services/agency-services';
import { AgencyLocation } from '../agency/agency-location/agency-location';
import { AgencySocialLinks } from '../agency/agency-social-links/agency-social-links';
import { SeoService } from '../../../services/seo.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { LoaderService } from '../../../services/loader.service';

export type UpgradeTenantData = {
  agencyName?: string;
  agencyPhone?: string;
  agencyCountryCode?: string;
  agencySize?: string;
  activeClients?: string;
  primaryServices?: string[];
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

const STEPS = ['المعلومات الأساسية', 'الحجم والعملاء', 'الخدمات', 'الموقع', 'روابط التواصل'];

@Component({
  selector: 'app-upgrade-tenant-page',
  imports: [
    RouterLink,
    SetupStepper,
    AgencyBasicInfo,
    AgencySizeClients,
    AgencyServices,
    AgencyLocation,
    AgencySocialLinks,
  ],
  templateUrl: './upgrade-tenant-page.html',
  styleUrl: './upgrade-tenant-page.css',
})
export class UpgradeTenantPage {
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  private readonly tenantService = inject(TenantService);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly loaderService = inject(LoaderService);
  private readonly storageKey = 'rawaj.upgrade-tenant';

  protected readonly step = signal(1);
  protected readonly data = signal<UpgradeTenantData>(this.loadData());

  protected readonly steps = STEPS;
  protected readonly totalSteps = computed(() => this.steps.length);

  constructor() {
    this.seo.setPageSeo({
      title: 'الترقية إلى وكالة تسويق | رواج',
      description: 'ارتقِ بحسابك ليصبح وكالة تسويق تدير أكثر من علامة تجارية.',
      keywords: 'رواج, ترقية الحساب, وكالة تسويق',
      path: '/upgrade-tenant',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected updateData(partial: Partial<UpgradeTenantData>): void {
    this.data.update(d => ({ ...d, ...partial }));
    this.saveData(this.data());
  }

  protected next(): void {
    this.step.update(s => s + 1);
  }

  protected back(): void {
    this.step.update(s => Math.max(1, s - 1));
  }

  protected finish(): void {
    const d = this.data();

    this.loaderService.show();
    this.tenantService
      .upgradeToAgency({
        agencySize: d.agencySize ?? '',
        servicesOffered: d.primaryServices ?? [],
        phone: d.agencyPhone,
        country: d.country,
        city: d.city,
        website: d.website,
      })
      .subscribe({
        next: res => {
          this.loaderService.hide();
          if (res.status !== 'success') {
            this.errorModalService.show(res.message ?? 'تعذّرت الترقية إلى وكالة تسويق.', { variant: 'error' });
            return;
          }
          localStorage.removeItem(this.storageKey);
          this.errorModalService.show('تهانينا! أصبح حسابك الآن وكالة تسويق بإمكانها إدارة عدة علامات تجارية.', {
            variant: 'success',
            title: 'تمت الترقية بنجاح',
          });
          this.router.navigate(['/dashboard/brand-profiles/new']);
        },
        error: () => {
          this.loaderService.hide();
          this.errorModalService.show('تعذّرت الترقية إلى وكالة تسويق. يرجى المحاولة مرة أخرى.', { variant: 'error' });
        },
      });
  }

  private loadData(): UpgradeTenantData {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return {};
    try { return JSON.parse(raw) as UpgradeTenantData; } catch { return {}; }
  }

  private saveData(data: UpgradeTenantData): void {
    localStorage.setItem(this.storageKey, JSON.stringify(data));
  }
}
