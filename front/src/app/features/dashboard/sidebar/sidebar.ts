import { Component, effect, inject, input, output, signal } from '@angular/core';
// signal kept for activeRoute
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { BillingApiService } from '../../../core/api/billing-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { AiCreditsUsage } from '../../../core/models';

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
  private readonly router = inject(Router);
  private readonly billingApi = inject(BillingApiService);
  private readonly tenantService = inject(TenantService);

  isOpen = input(true);
  mobileOpen = input(false);
  mobileClose = output<void>();

  activeRoute = signal('dashboard-home');

  readonly aiCreditsUsage = signal<AiCreditsUsage | null>(null);
  readonly aiCreditsPercent = signal(0);

  constructor() {
    effect(() => {
      if (this.tenantService.hasTenant()) {
        this.billingApi.getAiCreditsUsage().subscribe({
          next: (usage) => {
            this.aiCreditsUsage.set(usage);
            this.aiCreditsPercent.set(
              usage.maxCreditsMonthly > 0
                ? Math.min(100, Math.round((usage.usedThisMonth / usage.maxCreditsMonthly) * 100))
                : 0,
            );
          },
          error: () => this.aiCreditsUsage.set(null),
        });
      }
    });
  }

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
        { id: 'dashboard-home', label: 'لوحة التحكم',  icon: 'fa-gauge',        route: '/dashboard',           exact: true },
        { id: 'campaigns',      label: 'حملاتك',        icon: 'fa-bullhorn',     route: '/dashboard/campaigns' },
        { id: 'competitors',    label: 'تحليل المنافسين', icon: 'fa-chess-knight', route: '/dashboard/competitors' },
        { id: 'analytics',      label: 'التحليلات',      icon: 'fa-chart-line',   route: '/dashboard/analytics' },
      ],
    },
    {
      heading: 'إنشاء المحتوى',
      items: [
        { id: 'content-gen',    label: 'توليد المحتوى',    icon: 'fa-wand-magic-sparkles', route: '/dashboard/content-gen' },
        { id: 'my-media',       label: 'الوسائط الخاصة بي', icon: 'fa-photo-film',          route: '/dashboard/my-media' },
        { id: 'marketing-plan', label: 'خطتي التسويقية',   icon: 'fa-chart-gantt',         route: '/dashboard/marketing-plan' },
        { id: 'calendar',       label: 'تقويم المنشورات',  icon: 'fa-calendar-days',        route: '/dashboard/calendar' },
      ],
    },
    {
      heading: 'الفريق',
      items: [
        { id: 'users', label: 'المستخدمون', icon: 'fa-users-gear', route: '/dashboard/users' },
        { id: 'brand-profiles', label: 'العلامات التجارية', icon: 'fa-shop', route: '/dashboard/brand-profiles' },
      ],
    },
    {
      heading: 'الحساب',
      items: [
        { id: 'social-accounts', label: 'الحسابات المرتبطة', icon: 'fa-link',         route: '/dashboard/social-accounts' },
        { id: 'notifications', label: 'الإشعارات',        icon: 'fa-bell',            route: '/dashboard/notifications' },
        { id: 'billing',       label: 'الفوترة',          icon: 'fa-credit-card',     route: '/dashboard/billing' },
        { id: 'settings',      label: 'الإعدادات',        icon: 'fa-gear',            route: '/dashboard/settings' },
        { id: 'help',          label: 'المساعدة والدعم',  icon: 'fa-circle-question', route: '/dashboard/help' },
      ],
    },
  ];

  logout(): void {
    this.mobileClose.emit();
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
