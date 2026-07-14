import { Component, HostListener, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  mobileMenuOpen = input(false);

  toggleSidebar = output<void>();
  toggleMobileMenu = output<void>();

  searchQuery = signal('');
  notifOpen = signal(false);
  profileOpen = signal(false);
  fullscreen = signal(false);

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
}
