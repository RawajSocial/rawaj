import { Component, HostListener, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationsApiService } from '../../../core/api/notifications-api.service';
import { NotificationSummary } from '../../../core/models';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private readonly authService = inject(AuthService);
  private readonly notificationsApi = inject(NotificationsApiService);
  private readonly router = inject(Router);

  mobileMenuOpen = input(false);

  toggleSidebar = output<void>();
  toggleMobileMenu = output<void>();

  searchQuery = signal('');
  notifOpen = signal(false);
  profileOpen = signal(false);
  fullscreen = signal(false);

  readonly currentUser = this.authService.currentUser;
  readonly recentNotifications = signal<NotificationSummary[]>([]);
  readonly unreadCount = signal(0);

  constructor() {
    this.loadNotifications();
  }

  private loadNotifications(): void {
    this.notificationsApi.getAll(false, 1, 3).subscribe({
      next: (result) => {
        this.recentNotifications.set(result.items);
        this.unreadCount.set(result.items.filter((n) => !n.isRead).length);
      },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-dropdown')) {
      this.notifOpen.set(false);
    }
    if (!target.closest('.profile-dropdown')) {
      this.profileOpen.set(false);
    }
  }

  toggleFullscreen(): void {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      this.fullscreen.set(true);
    } else {
      document.exitFullscreen();
      this.fullscreen.set(false);
    }
  }

  logout(): void {
    this.profileOpen.set(false);
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
