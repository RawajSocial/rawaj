import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from '../../services/seo.service';
import { Header } from './header/header';
import { Sidebar } from './sidebar/sidebar';

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

  constructor(private readonly seo: SeoService) {}

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

  openMobileMenu(): void {
    this.mobileOverlayOpen.set(true);
  }

  closeMobileMenu(): void {
    this.mobileOverlayOpen.set(false);
  }
}
