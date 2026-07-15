import { NotificationCategory, NotificationType } from './enums';

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
