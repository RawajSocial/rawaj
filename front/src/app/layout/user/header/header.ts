import { Component, HostListener, computed, inject, input, output, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { AuthService } from '../../../core/auth/auth.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { TENANT_MEMBER_ROLE_LABELS } from '../../../model/tenant.model';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { BrandContextService } from '../../../services/brand-context.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, TooltipDirective, Avatar],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);
  protected readonly brandProfileService = inject(BrandProfileService);
  protected readonly brandContextService = inject(BrandContextService);

  mobileMenuOpen = input(false);

  toggleSidebar = output<void>();
  toggleMobileMenu = output<void>();

  protected readonly currentUser = this.authService.currentUser;
  protected readonly tenantRoleLabel = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role ? TENANT_MEMBER_ROLE_LABELS[role] : null;
  });

  searchQuery = signal('');
  notifOpen = signal(false);
  profileOpen = signal(false);
  fullscreen = signal(false);
  brandDropdownOpen = signal(false);
  campaignDropdownOpen = signal(false);

  // TODO: replace with the real balance once a billing/credits service exists.
  private readonly creditBalance = signal(2450);
  protected readonly creditBalanceLabel = computed(() => this.creditBalance().toLocaleString('ar-SA'));

  protected readonly selectedBrandLabel = computed(() =>
    this.brandContextService.selectedBrandProfile()?.name ?? 'اختر علامة تجارية',
  );

  protected readonly selectedCampaignLabel = computed(() => {
    const id = this.brandContextService.selectedCampaignId();
    if (id === 'all') return 'جميع الحملات';
    return this.brandContextService.campaignsForSelectedBrand().find(c => c.id === id)?.name ?? 'جميع الحملات';
  });

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-dropdown')) {
      this.notifOpen.set(false);
    }
    if (!target.closest('.profile-dropdown')) {
      this.profileOpen.set(false);
    }
    if (!target.closest('.brand-dropdown')) {
      this.brandDropdownOpen.set(false);
    }
    if (!target.closest('.campaign-dropdown')) {
      this.campaignDropdownOpen.set(false);
    }
  }

  protected selectBrandProfile(id: string): void {
    this.brandContextService.setBrandProfile(id);
    this.brandDropdownOpen.set(false);
  }

  protected selectCampaign(id: string | 'all'): void {
    this.brandContextService.setCampaign(id);
    this.campaignDropdownOpen.set(false);
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

  protected logout(): void {
    this.authService.logout().subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  private finishLogout(): void {
    this.tenantService.clear();
    this.router.navigate(['/login']);
  }
}
