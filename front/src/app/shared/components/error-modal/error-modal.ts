import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ModalShell } from '../modal-shell/modal-shell';
import { ErrorModalService } from '../../../services/error-modal.service';

@Component({
  selector: 'app-error-modal',
  imports: [ModalShell],
  templateUrl: './error-modal.html',
  styleUrl: './error-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorModal {
  private readonly errorModalService = inject(ErrorModalService);
  protected readonly state = this.errorModalService.state;

  protected close(): void {
    this.errorModalService.close();
  }
}
