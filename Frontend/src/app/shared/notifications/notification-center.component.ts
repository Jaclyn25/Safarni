import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, OnDestroy, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { UserNotification } from '../../models/notification.models';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-center.component.html',
  styleUrl: './notification-center.component.css'
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  readonly isOpen = signal(false);
  readonly isLoading = signal(false);
  readonly unreadCount = signal(0);
  readonly notifications = signal<UserNotification[]>([]);
  readonly error = signal('');
  private refreshTimer: number | null = null;

  constructor(
    private readonly notificationService: NotificationService,
    private readonly router: Router,
    private readonly elementRef: ElementRef<HTMLElement>
  ) {}

  ngOnInit(): void {
    this.load();
    this.refreshTimer = window.setInterval(() => this.load(false), 30000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer !== null) {
      window.clearInterval(this.refreshTimer);
    }
  }

  toggle(): void {
    this.isOpen.set(!this.isOpen());
    if (this.isOpen()) {
      this.load();
    }
  }

  open(notification: UserNotification): void {
    if (!notification.isRead) {
      this.notificationService.markRead(notification.id).subscribe({
        next: () => {
          notification.isRead = true;
          this.unreadCount.update((count) => Math.max(0, count - 1));
        }
      });
    }

    this.isOpen.set(false);
    if (notification.actionUrl?.startsWith('/')) {
      void this.router.navigateByUrl(notification.actionUrl);
    }
  }

  markAllRead(): void {
    this.notificationService.markAllRead().subscribe({
      next: () => {
        this.notifications.update((items) =>
          items.map((item) => ({ ...item, isRead: true }))
        );
        this.unreadCount.set(0);
      }
    });
  }

  icon(type: string): string {
    if (type.includes('Approved')) return 'verified';
    if (type.includes('Declined') || type.includes('Rejected')) return 'cancel';
    if (type.includes('Document')) return 'description';
    if (type.includes('Profile')) return 'storefront';
    if (type.includes('Suspended')) return 'block';
    return 'notifications';
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.isOpen.set(false);
    }
  }

  private load(showLoading = true): void {
    if (showLoading) this.isLoading.set(true);
    this.error.set('');
    this.notificationService.list().subscribe({
      next: (result) => {
        this.notifications.set(result.items);
        this.unreadCount.set(result.unreadCount);
        if (showLoading) this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Notifications are temporarily unavailable.');
        if (showLoading) this.isLoading.set(false);
      }
    });
  }
}
