import { Component, input, output, signal } from '@angular/core';
// signal kept for activeRoute
import { RouterLink, RouterLinkActive } from '@angular/router';

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
  isOpen = input(true);
  mobileOpen = input(false);
  mobileClose = output<void>();

  activeRoute = signal('dashboard-home');

  navSections: NavSection[] = [
    {
      heading: 'الرئيسية',
      items: [
        { id: 'dashboard-home', label: 'لوحة التحكم',  icon: 'fa-gauge',        route: '/dashboard',           exact: true },
        { id: 'campaigns',      label: 'حملاتك',        icon: 'fa-bullhorn',     route: '/dashboard/campaigns' },
        { id: 'ads',            label: 'إعلاناتك',      icon: 'fa-rectangle-ad', route: '/dashboard/ads' },
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
  ];

}
