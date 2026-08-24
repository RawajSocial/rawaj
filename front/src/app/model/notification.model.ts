/** Mirrors Rawaj.Domain.Enums.PlatformEnums — the real 7-category notification system. */
export type NotificationType = 'Info' | 'Success' | 'Warning' | 'Error';
export type NotificationCategory =
  'AiJob' | 'PostPublished' | 'PostFailed' | 'ReviewNeeded' | 'Billing' | 'System' | 'TeamInvite';

/** GET /api/v1/notifications — Rawaj.Application.Features.Notifications.NotificationSummary. */
export interface NotificationSummary {
  id: string;
  type: NotificationType;
  category: NotificationCategory;
  title: string;
  message: string;
  refId: string | null;
  refType: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export const CATEGORY_CFG: Record<NotificationCategory, { icon: string; bg: string; color: string; label: string }> = {
  AiJob:         { icon: 'fa-wand-magic-sparkles', bg: 'var(--color-secondary-subtle)', color: 'var(--color-secondary)', label: 'الذكاء الاصطناعي' },
  PostPublished: { icon: 'fa-circle-check',        bg: 'var(--color-success-light)',    color: 'var(--color-success)',   label: 'نشر منشور' },
  PostFailed:    { icon: 'fa-triangle-exclamation', bg: 'var(--color-danger-light)',     color: 'var(--color-danger)',    label: 'فشل نشر' },
  ReviewNeeded:  { icon: 'fa-eye',                 bg: 'var(--color-info-light)',       color: 'var(--color-info)',      label: 'بانتظار المراجعة' },
  Billing:       { icon: 'fa-credit-card',         bg: 'var(--color-success-light)',    color: 'var(--color-success)',   label: 'الفوترة' },
  System:        { icon: 'fa-gear',                bg: 'var(--color-warning-light)',    color: 'var(--color-accent-active)', label: 'النظام' },
  TeamInvite:    { icon: 'fa-user-plus',            bg: 'var(--color-info-light)',       color: 'var(--color-info)',      label: 'دعوة فريق' },
};

export interface NotificationLink {
  commands: unknown[];
  queryParams?: Record<string, string>;
}

/** Deep-links a notification to the page its RefType/RefId describe. Only whitelisted RefTypes
 *  resolve — a notification whose ref can't be located on any known route (e.g. a bare
 *  `content_item` with no campaign id to build a URL from) returns null and renders unlinked
 *  rather than guessing a broken link. */
export function notificationLink(n: NotificationSummary): NotificationLink | null {
  if (!n.refId) return null;
  switch (n.refType) {
    case 'marketing_campaign':
      return { commands: ['/dashboard/campaigns', n.refId] };
    case 'scheduled_post':
      // No campaign id on the notification to build the nested campaign-post-detail route —
      // the calendar is the one place a bare scheduled-post id can be resolved from.
      return { commands: ['/dashboard/calendar'] };
    case 'tenant_member':
      // AddTeamMemberCommandHandler is the only place this ref type is emitted (RefId is the
      // TenantMember.Id), so this always means "you were invited" — route to the accept/decline
      // page rather than the (inaccessible, since the invited tenant isn't active yet) team list.
      return n.category === 'TeamInvite'
        ? { commands: ['/invite'], queryParams: { token: n.refId } }
        : { commands: ['/dashboard/users'] };
    default:
      return null;
  }
}
