export interface UserNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  actionUrl: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

export interface UserNotificationList {
  unreadCount: number;
  items: UserNotification[];
}
