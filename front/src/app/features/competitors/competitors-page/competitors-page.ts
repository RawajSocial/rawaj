import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CompetitorsApiService } from '../../../core/api/competitors-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { ApiError } from '../../../core/api';
import { AddCompetitorModal, AddCompetitorValue } from '../add-competitor-modal/add-competitor-modal';
import { CompetitorDetailModal } from '../competitor-detail-modal/competitor-detail-modal';
import { CompetitorSummary } from '../../../core/models';

@Component({
  selector: 'app-competitors-page',
  imports: [PageHeader, AddCompetitorModal, CompetitorDetailModal],
  templateUrl: './competitors-page.html',
  styleUrls: [
    '../../dashboard/dashboard-shared.css',
    '../../campaigns/campaigns-page/campaigns-page.css',
    './competitors-page.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompetitorsPage {
  private readonly competitorsApi = inject(CompetitorsApiService);
  private readonly tenantService = inject(TenantService);

  protected readonly competitors = signal<CompetitorSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly addModalOpen = signal(false);
  protected readonly adding = signal(false);
  protected readonly addError = signal<string | null>(null);

  protected readonly selectedCompetitor = signal<CompetitorSummary | null>(null);

  constructor() {
    effect(() => {
      const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
      if (brandProfileId) {
        this.load(brandProfileId);
      }
    });
  }

  private load(brandProfileId: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.competitorsApi.getByBrand(brandProfileId, 1, 50).subscribe({
      next: (result) => {
        this.competitors.set(result.items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل قائمة المنافسين.');
      },
    });
  }

  protected openAddModal(): void {
    this.addError.set(null);
    this.addModalOpen.set(true);
  }

  protected onAddSaved(value: AddCompetitorValue): void {
    const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
    if (!brandProfileId) return;

    this.adding.set(true);
    this.addError.set(null);

    this.competitorsApi.add({ brandProfileId, name: value.name, url: value.url }).subscribe({
      next: () => {
        this.adding.set(false);
        this.addModalOpen.set(false);
        this.load(brandProfileId);
      },
      error: (error: unknown) => {
        this.adding.set(false);
        this.addError.set(error instanceof ApiError ? error.message : 'تعذر إضافة المنافس، حاول مرة أخرى.');
      },
    });
  }

  protected openDetail(competitor: CompetitorSummary): void {
    this.selectedCompetitor.set(competitor);
  }

  protected closeDetail(): void {
    this.selectedCompetitor.set(null);
  }

  protected onAnalyzed(): void {
    const brandProfileId = this.tenantService.activeBrandProfile()?.brandProfileId;
    if (brandProfileId) this.load(brandProfileId);
  }

  protected formatDate(iso: string | null): string {
    if (!iso) return 'لم يُحلَّل بعد';
    return new Date(iso).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
