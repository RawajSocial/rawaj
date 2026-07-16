import { Component, OnInit, effect, signal } from '@angular/core';
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

  constructor(private readonly seo: SeoService) {
    effect(() => {
      document.body.classList.toggle('no-scroll', this.mobileOverlayOpen());
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
