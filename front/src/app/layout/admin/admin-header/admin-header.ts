import { Component, HostListener, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

@Component({
  selector: 'app-admin-header',
  standalone: true,
  imports: [RouterLink, TooltipDirective],
  templateUrl: './admin-header.html',
  styleUrl: './admin-header.css',
})
export class AdminHeader {
  mobileMenuOpen = input(false);

  toggleSidebar = output<void>();
  toggleMobileMenu = output<void>();

  searchQuery = signal('');
  profileOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.admin-profile-dropdown')) {
      this.profileOpen.set(false);
    }
  }
}
