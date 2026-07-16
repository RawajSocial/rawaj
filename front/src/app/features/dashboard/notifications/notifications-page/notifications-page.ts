import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { SeoService } from '../../../../services/seo.service';

type NotifType = 'campaign' | 'content' | 'team' | 'billing' | 'system';
type NotifTab = 'all' | 'unread' | 'read';

interface AppNotification {
  id: string;
  type: NotifType;
  title: string;
  body: string;
  time: string;
  read: boolean;
}

const TYPE_CFG: Record<NotifType, { icon: string; bg: string; color: string }> = {
  campaign: { icon: 'fa-bullhorn',        bg: 'var(--color-primary-subtle)',   color: 'var(--color-primary)' },
  content:  { icon: 'fa-wand-magic-sparkles', bg: 'var(--color-secondary-subtle)', color: 'var(--color-secondary)' },
  team:     { icon: 'fa-user-plus',       bg: 'var(--color-info-light)',       color: 'var(--color-info)' },
  billing:  { icon: 'fa-credit-card',     bg: 'var(--color-success-light)',    color: 'var(--color-success)' },
  system:   { icon: 'fa-gear',            bg: 'var(--color-warning-light)',    color: 'var(--color-accent-active)' },
};

@Component({
  selector: 'app-notifications-page',
  imports: [PageHeader],
  templateUrl: './notifications-page.html',
  styleUrls: ['../../dashboard-shared.css', './notifications-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {
  private readonly seo = inject(SeoService);

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
  }

  protected readonly typeCfg = TYPE_CFG;

  protected readonly notifications = signal<AppNotification[]>([
    { id: 'n1', type: 'content',  title: 'اكتمل توليد المحتوى', body: 'تم إنشاء 8 منشورات جديدة لحملة "رمضان الكريم".', time: 'منذ دقيقتين', read: false },
    { id: 'n2', type: 'campaign', title: 'حملة قيد النشر',      body: 'بدأ نشر حملة "إطلاق منتج العيد" على إنستغرام وفيسبوك.', time: 'منذ 15 دقيقة', read: false },
    { id: 'n3', type: 'team',     title: 'عضو جديد انضم',       body: 'قبل عمر خالد دعوتك للانضمام إلى الوكالة.', time: 'منذ ساعة', read: false },
    { id: 'n4', type: 'billing',  title: 'تم تجديد الاشتراك',    body: 'تم تجديد باقة الأساسية بنجاح لشهر جديد.', time: 'منذ 3 ساعات', read: false },
    { id: 'n5', type: 'campaign', title: 'أداء منشور مميز',      body: 'تجاوز منشورك على تيك توك 100 ألف مشاهدة.', time: 'منذ 5 ساعات', read: true },
    { id: 'n6', type: 'system',   title: 'ربط منصة جديد',        body: 'تم ربط حساب لينكدإن بنجاح بمنصة رواج.', time: 'أمس', read: true },
    { id: 'n7', type: 'content',  title: 'منشور بانتظار المراجعة', body: 'يوجد منشور مجدول يحتاج إلى مراجعتك قبل النشر.', time: 'أمس', read: true },
  ]);

  protected readonly tab = signal<NotifTab>('all');
  protected readonly unreadCount = computed(() => this.notifications().filter(n => !n.read).length);

  protected readonly filtered = computed(() => {
    const t = this.tab();
    return this.notifications().filter(n => t === 'all' || (t === 'unread' ? !n.read : n.read));
  });

  protected setTab(t: NotifTab): void { this.tab.set(t); }

  protected markAllRead(): void {
    this.notifications.update(list => list.map(n => ({ ...n, read: true })));
  }

  protected toggleRead(id: string): void {
    this.notifications.update(list => list.map(n => (n.id === id ? { ...n, read: !n.read } : n)));
  }
}
