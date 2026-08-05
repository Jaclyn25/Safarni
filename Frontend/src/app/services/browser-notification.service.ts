import { Injectable } from '@angular/core';

export type NotificationPermissionStatus =
  | NotificationPermission
  | 'unsupported';

@Injectable({ providedIn: 'root' })
export class BrowserNotificationService {
  isSupported(): boolean {
    return typeof window !== 'undefined' && 'Notification' in window;
  }

  getPermission(): NotificationPermissionStatus {
    if (!this.isSupported()) {
      return 'unsupported';
    }
    return Notification.permission;
  }

  async requestPermission(): Promise<NotificationPermissionStatus> {
    if (!this.isSupported()) {
      return 'unsupported';
    }
    return Notification.requestPermission();
  }

  notify(title: string, body: string, url?: string): void {
    if (!this.isSupported() || Notification.permission !== 'granted') {
      return;
    }

    const notification = new Notification(title, {
      body,
      icon: 'assets/logo-icon.svg',
      tag: 'safarni-chat'
    });

    if (url) {
      notification.onclick = () => {
        notification.close();
        window.focus();
        window.location.assign(url);
      };
    }
  }
}
