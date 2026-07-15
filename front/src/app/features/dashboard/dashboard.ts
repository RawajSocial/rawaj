import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from '../../services/seo.service';
import { Header } from './header/header';
import { Sidebar } from './sidebar/sidebar';
import { TenantService } from '../../core/tenant/tenant.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [Header, Sidebar, RouterOutlet],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  sidebarOpen = signal(true);
  mobileOverlayOpen = signal(false);

  private readonly tenantService = inject(TenantService);

  constructor(private readonly seo: SeoService) {
    effect(() => {
      document.body.classList.toggle('no-scroll', this.mobileOverlayOpen());
    });

    // Re-hydrates tenant/brand context on a hard page refresh - loadContext() after
    // login/register only covers the in-app navigation case.
    if (!this.tenantService.loaded()) {
      this.tenantService.loadContext().subscribe();
    }
  }

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'Dashboard | Rawaj',
      description:
        'Track campaign performance, content workflows, and account activity from the Rawaj dashboard.',
      keywords: 'Rawaj dashboard, campaign analytics, marketing performance, account overview',
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
