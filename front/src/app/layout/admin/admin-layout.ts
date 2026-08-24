import { Component, OnInit, effect, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SeoService } from '../../services/seo.service';
import { AdminHeader } from './admin-header/admin-header';
import { AdminSidebar } from './admin-sidebar/admin-sidebar';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [AdminHeader, AdminSidebar, RouterOutlet],
  templateUrl: './admin-layout.html',
  styleUrls: ['./admin-theme.css', './admin-layout.css'],
})
export class AdminLayout implements OnInit {
  sidebarOpen = signal(true);
  mobileOverlayOpen = signal(false);

  constructor(private readonly seo: SeoService) {
    effect(() => {
      document.body.classList.toggle('no-scroll', this.mobileOverlayOpen());
    });
  }

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'لوحة إدارة المنصة | رواج',
      description: 'إدارة المستخدمين والوكالات والاشتراكات وإعدادات منصة رواج.',
      keywords: 'رواج, إدارة المنصة, لوحة الأدمن',
      path: '/admin',
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
