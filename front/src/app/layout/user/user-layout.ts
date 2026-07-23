import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SeoService } from '../../services/seo.service';
import { TenantService } from '../../core/tenant/tenant.service';
import { BrandProfileService } from '../../services/brand-profile.service';
import { CampaignService } from '../../services/campaign.service';
import { BrandContextService } from '../../services/brand-context.service';
import { Header } from './header/header';
import { Sidebar } from './sidebar/sidebar';

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [Header, Sidebar, RouterOutlet],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.css',
})
export class UserLayout implements OnInit {
  sidebarOpen = signal(true);
  mobileOverlayOpen = signal(false);

  private readonly tenantService = inject(TenantService);
  private readonly brandProfileService = inject(BrandProfileService);
  private readonly campaignService = inject(CampaignService);
  private readonly brandContextService = inject(BrandContextService);

  constructor(private readonly seo: SeoService) {
    effect(() => {
      document.body.classList.toggle('no-scroll', this.mobileOverlayOpen());
    });

    if (!this.tenantService.tenant()) {
      this.tenantService.refreshMemberships().subscribe();
    }

    // Load brand profiles + campaigns once, then pick a default brand for the
    // global header selector (only if nothing is already selected/persisted).
    forkJoin([this.brandProfileService.refresh(), this.campaignService.refresh()]).subscribe({
      next: () => this.brandContextService.initDefault(),
      error: () => { /* header selectors just stay empty; pages surface their own errors */ },
    });
  }

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'لوحة التحكم | رواج',
      description:
        'تابع أداء حملاتك، ومحتواك، ونشاط حسابك من لوحة تحكم رواج.',
      keywords: 'رواج, لوحة التحكم, تحليلات الحملات, أداء التسويق',
      path: '/dashboard',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  toggleMobileMenu(): void {
    this.mobileOverlayOpen.update(v => !v);
  }

  closeMobileMenu(): void {
    this.mobileOverlayOpen.set(false);
  }
}
