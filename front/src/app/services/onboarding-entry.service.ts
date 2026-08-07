import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CampaignService } from './campaign.service';
import { BrandContextService } from './brand-context.service';
import { ConfirmDialogService } from './confirm-dialog.service';
import { ErrorModalService } from './error-modal.service';
import { extractApiErrorMessage } from '../core/auth/api-error.util';

/**
 * Decides where "ابدأ حملة جديدة" actually goes, instead of the three call sites
 * (campaigns-page, ads-page, marketing-plan-page) each unconditionally sending the user to a
 * brand-new `/on-boarding?fresh=1` draft.
 *
 * Doing that unconditionally was the root of two bugs: it silently discarded whatever unfinished
 * draft the user already had (no warning, no way back to it), and every discard left the old Draft
 * campaign row behind forever — `fresh=1` only clears the client-side pointer, never archives the
 * campaign it pointed at.
 */
@Injectable({ providedIn: 'root' })
export class OnboardingEntryService {
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly brandContextService = inject(BrandContextService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly errorModalService = inject(ErrorModalService);

  /** Callers are responsible for their own permission/brand-profile-existence checks first (as the
   *  three call sites already did before this existed) — this only decides where to send the user
   *  once that's established. */
  async startOrResumeOnboarding(): Promise<void> {
    const brandId = this.brandContextService.selectedBrandProfileId();
    const existingDraft = brandId ? this.findUnfinishedDraft(brandId) : null;

    if (!existingDraft) {
      void this.router.navigate(['/on-boarding'], { queryParams: { fresh: 1 } });
      return;
    }

    // Dismissing (Esc/backdrop) resolves the same as the cancel button — both must be the SAFE
    // option here. Losing an unfinished campaign's progress must never be one accidental click away.
    const startNew = await this.confirmDialogService.confirm(
      `لديك حملة غير مكتملة باسم "${existingDraft.name}". هل تريد المتابعة منها أم البدء من جديد؟`,
      { title: 'حملة غير مكتملة', confirmLabel: 'بدء حملة جديدة', cancelLabel: 'متابعة الحملة السابقة' },
    );

    if (startNew) {
      this.campaignService.archive(existingDraft.id).subscribe({
        next: () => void this.router.navigate(['/on-boarding'], { queryParams: { fresh: 1 } }),
        // Left on the current page rather than navigating anyway: a wizard opened with `fresh=1`
        // right now would create yet another draft alongside the one that failed to archive —
        // exactly the orphaning this whole feature exists to stop.
        error: err => this.errorModalService.show(
          extractApiErrorMessage(err, 'تعذّر إغلاق الحملة السابقة. حاول مرة أخرى.'), { variant: 'error' }),
      });
      return;
    }

    void this.router.navigate(
      existingDraft.onboardingCompletedAt
        ? ['/dashboard/campaigns', existingDraft.id, 'strategy']
        : ['/on-boarding'],
      existingDraft.onboardingCompletedAt ? undefined : { queryParams: { resume: existingDraft.id } },
    );
  }

  private findUnfinishedDraft(brandProfileId: string) {
    const candidates = this.campaignService.byBrandProfile(brandProfileId)()
      .filter(c => c.status === 'draft' && !c.planApprovedAt);
    if (candidates.length === 0) return null;
    return candidates.reduce((latest, c) => (c.createdAt > latest.createdAt ? c : latest));
  }
}
