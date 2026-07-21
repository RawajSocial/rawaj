import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { BrandProfileFormModal } from '../brand-profile-form-modal/brand-profile-form-modal';
import { BrandMembersModal } from '../brand-members-modal/brand-members-modal';
import { TenantService } from '../../../../core/tenant/tenant.service';
import { BrandProfileSummary } from '../../../../core/models';

@Component({
  selector: 'app-brand-profiles-page',
  imports: [PageHeader, BrandProfileFormModal, BrandMembersModal],
  templateUrl: './brand-profiles-page.html',
  styleUrl: './brand-profiles-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandProfilesPage {
  protected readonly tenantService = inject(TenantService);

  protected readonly canManageBrands = computed(() => {
    const role = this.tenantService.tenant()?.role;
    return role === 'Owner' || role === 'Admin';
  });

  protected readonly formModalOpen = signal(false);
  protected readonly editingBrand = signal<BrandProfileSummary | null>(null);

  protected readonly membersModalOpen = signal(false);
  protected readonly membersModalBrand = signal<BrandProfileSummary | null>(null);

  protected openCreate(): void {
    this.editingBrand.set(null);
    this.formModalOpen.set(true);
  }

  protected openEdit(brand: BrandProfileSummary): void {
    this.editingBrand.set(brand);
    this.formModalOpen.set(true);
  }

  protected closeModal(): void {
    this.formModalOpen.set(false);
  }

  protected onSaved(): void {
    this.formModalOpen.set(false);
    this.tenantService.refreshBrandProfiles().subscribe();
  }

  protected isActive(brand: BrandProfileSummary): boolean {
    return this.tenantService.activeBrandProfile()?.brandProfileId === brand.brandProfileId;
  }

  protected setActive(brand: BrandProfileSummary): void {
    this.tenantService.setActiveBrandProfile(brand.brandProfileId);
  }

  protected openMembers(brand: BrandProfileSummary): void {
    this.membersModalBrand.set(brand);
    this.membersModalOpen.set(true);
  }

  protected closeMembers(): void {
    this.membersModalOpen.set(false);
  }
}
