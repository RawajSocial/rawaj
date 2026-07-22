import { Component, inject, input, output, signal } from '@angular/core';
// signal kept for activeRoute
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { TenantService } from '../../../core/tenant/tenant.service';

type NavItem = {
  id: string;
  label: string;
  icon: string;
  route?: string;
  exact?: boolean;
  badge?: string;
  children?: NavItem[];
};

type NavSection = {
  heading: string;
  items: NavItem[];
};

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);

  isOpen = input(true);
  mobileOpen = input(false);
  mobileClose = output<void>();

  activeRoute = signal('dashboard-home');

  private readonly collapsedSections = signal<Set<string>>(new Set());

  isSectionCollapsed(heading: string): boolean {
    return this.collapsedSections().has(heading);
  }

  toggleSection(heading: string): void {
    this.collapsedSections.update(current => {
      const next = new Set(current);
      if (next.has(heading)) next.delete(heading);
      else next.add(heading);
      return next;
    });
  }

  navSections: NavSection[] = [
    {
      heading: 'الرئيسية',
      items: [
        { id: 'dashboard-home',  label: 'لوحة التحكم',        icon: 'fa-gauge',        route: '/dashboard',           exact: true },
        { id: 'brand-profiles',  label: 'ملفات العلامة التجارية', icon: 'fa-copyright', route: '/dashboard/brand-profiles' },
        { id: 'campaigns',       label: 'حملاتك',              icon: 'fa-bullhorn',     route: '/dashboard/campaigns' },
        { id: 'ads',             label: 'منشوراتك',            icon: 'fa-rectangle-ad', route: '/dashboard/ads' },
      ],
    },
    {
      heading: 'إنشاء المحتوى',
      items: [
        { id: 'content-gen',    label: 'توليد المحتوى',    icon: 'fa-wand-magic-sparkles', route: '/dashboard/content-gen' },
        { id: 'my-media',       label: 'إعلاناتي',          icon: 'fa-photo-film',          route: '/dashboard/my-media' },
        { id: 'marketing-plan', label: 'خطتي التسويقية',   icon: 'fa-chart-gantt',         route: '/dashboard/marketing-plan' },
        { id: 'calendar',       label: 'تقويم المنشورات',  icon: 'fa-calendar-days',        route: '/dashboard/calendar' },
      ],
    },
    {
      heading: 'الفريق',
      items: [
        { id: 'my-projects', label: 'مشاريعي',      icon: 'fa-diagram-project', route: '/dashboard/my-projects' },
        { id: 'users',       label: 'المستخدمون',   icon: 'fa-users-gear',      route: '/dashboard/users' },
      ],
    },
    {
      heading: 'الحساب',
      items: [
        { id: 'notifications', label: 'الإشعارات',        icon: 'fa-bell',            route: '/dashboard/notifications' },
        { id: 'billing',       label: 'الفوترة',          icon: 'fa-credit-card',     route: '/dashboard/billing' },
        { id: 'settings',      label: 'الإعدادات',        icon: 'fa-gear',            route: '/dashboard/settings' },
        { id: 'help',          label: 'المساعدة والدعم',  icon: 'fa-circle-question', route: '/dashboard/help' },
      ],
    },
  ];

  protected logout(): void {
    this.authService.logout().subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  private finishLogout(): void {
    this.tenantService.clear();
    this.mobileClose.emit();
    this.router.navigate(['/login']);
  }
}
