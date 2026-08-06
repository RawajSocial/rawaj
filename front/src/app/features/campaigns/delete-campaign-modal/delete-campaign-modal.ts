import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';
import { CampaignDeleteSummary } from '../../../model/campaign.model';

/**
 * The delete-confirmation breakdown for a campaign — a dedicated modal rather than the generic
 * `ConfirmDialogService` text prompt archive/other actions use, because this action is irreversible
 * in practice (soft-deleted, not recoverable from the UI) and cascades to real data: posts, images,
 * and any pending scheduled posts (which are actually cancelled on their platform, not just hidden
 * locally). The user should see exactly what's about to happen, not a one-line warning.
 */
@Component({
  selector: 'app-delete-campaign-modal',
  imports: [ModalShell],
  templateUrl: './delete-campaign-modal.html',
  styleUrl: './delete-campaign-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteCampaignModal {
  readonly open = input.required<boolean>();
  /** Null while the summary is still loading. */
  readonly summary = input<CampaignDeleteSummary | null>(null);
  readonly deleting = input(false);

  readonly confirmDelete = output<void>();
  readonly cancel = output<void>();
}
