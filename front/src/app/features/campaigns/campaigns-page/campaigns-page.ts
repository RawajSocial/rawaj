import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CampaignCard } from '../campaign-card/campaign-card';
import { CampaignStatus, CampaignPlatform } from '../../../model/campaign.model';
import { SeoService } from '../../../services/seo.service';
import { CampaignService } from '../../../services/campaign.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';
import { Breadcrumb } from '../../../shared/components/breadcrumb/breadcrumb';
import { BrandLock } from '../../../shared/components/brand-lock/brand-lock';
import { TenantService } from '../../../core/tenant/tenant.service';

@Component({
  selector: 'app-campaigns-page',
  standalone: true,
  imports: [CampaignCard, Breadcrumb, BrandLock],
  templateUrl: './campaigns-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './campaigns-page.css'],
})
export class CampaignsPage {
  private readonly seo = inject(SeoService);
  private readonly campaignService = inject(CampaignService);
  private readonly router = inject(Router);
  private readonly errorModalService = inject(ErrorModalService);
  private readonly tenantService = inject(TenantService);

  constructor() {
    this.seo.setPageSeo({
      title: 'الحملات التسويقية | رواج',
      description: 'أنشئ حملاتك التسويقية وتابع أداءها وميزانيتها في مكان واحد.',
      keywords: 'رواج, حملات تسويقية, إدارة حملات, ميزانية إعلانية',
      path: '/dashboard/campaigns',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected readonly campaigns = this.campaignService.campaigns;
  protected readonly brandProfileCount = this.tenantService.brandProfileCount;
  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<CampaignStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!(e.target as HTMLElement).closest('[data-dd="platform"]')) this.platformOpen.set(false);
  }

  protected setStatus(v: CampaignStatus | 'all'): void   { this.statusFilter.set(v);   this.statusOpen.set(false); }
  protected setPlatform(v: CampaignPlatform | 'all'): void { this.platformFilter.set(v); this.platformOpen.set(false); }

  protected readonly statusOptions: { value: CampaignStatus | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع الحالات' },
    { value: 'active',    label: 'نشطة' },
    { value: 'paused',    label: 'موقوفة' },
    { value: 'completed', label: 'مكتملة' },
    { value: 'draft',     label: 'مسودة' },
  ];

  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع المنصات' },
    { value: 'instagram', label: 'إنستغرام' },
    { value: 'facebook',  label: 'فيسبوك' },
    { value: 'tiktok',    label: 'تيك توك' },
    { value: 'youtube',   label: 'يوتيوب' },
    { value: 'snapchat',  label: 'سناب شات' },
    { value: 'linkedin',  label: 'لينكد إن' },
    { value: 'x',         label: 'إكس (تويتر)' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    return this.campaigns().filter(c => {
      if (q  && !c.name.toLowerCase().includes(q))             return false;
      if (st !== 'all' && c.status !== st)                     return false;
      if (pl !== 'all' && !c.platforms.includes(pl))           return false;
      return true;
    });
  });

  protected get statusLabel():   string { return this.statusOptions.find(o => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }

  protected pauseCampaign(id: string): void {
    if (!this.requireBrandProfile()) return;
    this.campaignService.update(id, { status: 'Paused' }).subscribe({
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إيقاف الحملة.'), { variant: 'error' }),
    });
  }

  protected resumeCampaign(id: string): void {
    if (!this.requireBrandProfile()) return;
    this.campaignService.update(id, { status: 'Active' }).subscribe({
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر تفعيل الحملة.'), { variant: 'error' }),
    });
  }

  protected viewCampaign(id: string): void {
    this.router.navigate(['/dashboard/campaigns', id]);
  }

  protected startNewCampaign(): void {
    if (!this.requireBrandProfile()) return;
    this.router.navigate(['/on-boarding']);
  }

  private requireBrandProfile(): boolean {
    if (this.tenantService.brandProfileCount() > 0) return true;
    this.errorModalService.show(
      'يجب إنشاء ملف علامة تجارية أولاً لاستخدام هذه الميزة.',
      { variant: 'warning', title: 'يلزم إنشاء ملف علامة تجارية' },
    );
    this.router.navigate(['/dashboard/brand-profiles/new']);
    return false;
  }
}
