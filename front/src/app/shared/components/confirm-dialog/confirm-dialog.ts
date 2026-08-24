import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ModalShell } from '../modal-shell/modal-shell';
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  imports: [ModalShell],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
  private readonly confirmDialogService = inject(ConfirmDialogService);
  protected readonly state = this.confirmDialogService.state;

  protected respond(result: boolean): void {
    this.confirmDialogService.respond(result);
  }
}
