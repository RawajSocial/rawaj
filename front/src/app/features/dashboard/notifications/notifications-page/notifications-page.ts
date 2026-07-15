import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { NotificationsApiService } from '../../../../core/api/notifications-api.service';
import { ApiError } from '../../../../core/api';
import { NotificationCategory, NotificationSummary } from '../../../../core/models';

type NotifTab = 'all' | 'unread' | 'read';

const CATEGORY_CFG: Record<NotificationCategory, { icon: string; bg: string; color: string }> = {
  AiJob:         { icon: 'fa-wand-magic-sparkles', bg: 'var(--color-secondary-subtle)', color: 'var(--color-secondary)' },
  PostPublished: { icon: 'fa-bullhorn',            bg: 'var(--color-primary-subtle)',   color: 'var(--color-primary)' },
  PostFailed:    { icon: 'fa-triangle-exclamation', bg: 'var(--color-warning-light)',   color: 'var(--color-accent-active)' },
  ReviewNeeded:  { icon: 'fa-eye',                 bg: 'var(--color-info-light)',       color: 'var(--color-info)' },
  Billing:       { icon: 'fa-credit-card',         bg: 'var(--color-success-light)',    color: 'var(--color-success)' },
  System:        { icon: 'fa-gear',                bg: 'var(--color-warning-light)',    color: 'var(--color-accent-active)' },
};

@Component({
  selector: 'app-notifications-page',
  imports: [PageHeader],
  templateUrl: './notifications-page.html',
  styleUrls: ['../../dashboard-shared.css', './notifications-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {
  private readonly notificationsApi = inject(NotificationsApiService);

  protected readonly categoryCfg = CATEGORY_CFG;

  protected readonly notifications = signal<NotificationSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly tab = signal<NotifTab>('all');
  protected readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  protected readonly filtered = computed(() => {
    const t = this.tab();
    return this.notifications().filter(n => t === 'all' || (t === 'unread' ? !n.isRead : n.isRead));
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.notificationsApi.getAll(false, 1, 50).subscribe({
      next: (result) => {
        this.notifications.set(result.items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الإشعارات.');
      },
    });
  }

  protected setTab(t: NotifTab): void { this.tab.set(t); }

  protected markAllRead(): void {
    this.notificationsApi.markAllRead().subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true, readAt: n.readAt ?? new Date().toISOString() })));
      },
    });
  }

  protected markRead(id: string): void {
    const notification = this.notifications().find(n => n.id === id);
    if (!notification || notification.isRead) return;

    this.notificationsApi.markRead(id).subscribe({
      next: () => {
        this.notifications.update(list =>
          list.map(n => (n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n)),
        );
      },
    });
  }

  protected relativeTime(iso: string): string {
    const diffMs = Date.now() - new Date(iso).getTime();
    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'الآن';
    if (minutes < 60) return `منذ ${minutes} دقيقة`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `منذ ${hours} ساعة`;
    const days = Math.floor(hours / 24);
    if (days === 1) return 'أمس';
    return `منذ ${days} يوم`;
  }
}
