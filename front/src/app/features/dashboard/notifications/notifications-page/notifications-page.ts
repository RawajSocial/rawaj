import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';
import { NotificationService } from '../../../../services/notification.service';
import { CATEGORY_CFG, NotificationLink, notificationLink } from '../../../../model/notification.model';

type NotifTab = 'all' | 'unread' | 'read';

@Component({
  selector: 'app-notifications-page',
  imports: [PageHeader],
  templateUrl: './notifications-page.html',
  styleUrls: ['../../dashboard-shared.css', './notifications-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {
  private readonly seo = inject(SeoService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);

  protected readonly categoryCfg = CATEGORY_CFG;
  protected readonly notificationLink = notificationLink;

  protected readonly items = this.notificationService.items;
  protected readonly loading = this.notificationService.loading;
  protected readonly unreadCount = this.notificationService.unreadCount;

  protected readonly tab = signal<NotifTab>('all');

  constructor() {
    this.seo.setPageSeo({
      title: 'الإشعارات | رواج',
      description: 'تابع كل التحديثات المتعلقة بحملاتك ومحتواك وفريقك.',
      keywords: 'رواج, إشعارات, تنبيهات الحملات, تحديثات',
      path: '/dashboard/notifications',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    this.load();
  }

  /** Loads the first page of ALL notifications once. 'unread'/'read' tabs then filter that same
   *  loaded page client-side (see `filtered`) — 'read' has no server-side flag to request, so with
   *  the default page size this is an honest partial view (only what's on this page), not a bug. */
  private load(): void {
    this.notificationService.refresh().subscribe();
  }

  protected readonly filtered = computed(() => {
    const t = this.tab();
    const items = this.items();
    if (t === 'all') return items;
    if (t === 'unread') return items.filter(n => !n.isRead);
    return items.filter(n => n.isRead);
  });

  protected setTab(t: NotifTab): void {
    this.tab.set(t);
  }

  protected markAllRead(): void {
    this.notificationService.markAllRead().subscribe();
  }

  protected openNotification(id: string, link: NotificationLink | null): void {
    this.notificationService.markRead(id).subscribe();
    if (link) void this.router.navigate(link.commands, { queryParams: link.queryParams });
  }

  protected formatTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }
}
