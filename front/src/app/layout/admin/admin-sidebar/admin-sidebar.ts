import { Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface AdminNavItem {
  id: string;
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './admin-sidebar.html',
  styleUrl: './admin-sidebar.css',
})
export class AdminSidebar {
  mobileOpen = input(false);
  mobileClose = output<void>();

  readonly navItems: AdminNavItem[] = [
    { id: 'overview', label: 'نظرة عامة',                  icon: 'fa-gauge-high',        route: '/admin', exact: true },
    { id: 'users',    label: 'المستخدمون',                  icon: 'fa-users',             route: '/admin/users' },
    { id: 'tenants',  label: 'الوكالات والعلامات التجارية',   icon: 'fa-building',           route: '/admin/tenants' },
    { id: 'plans',    label: 'الاشتراكات والباقات',           icon: 'fa-credit-card',        route: '/admin/plans' },
    { id: 'settings', label: 'الإعدادات العامة',              icon: 'fa-gear',               route: '/admin/settings' },
  ];
}
