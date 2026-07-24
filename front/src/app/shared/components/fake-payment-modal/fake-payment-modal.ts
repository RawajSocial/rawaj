import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ModalShell } from '../modal-shell/modal-shell';
import { FakePaymentModalService } from '../../../services/fake-payment-modal.service';

@Component({
  selector: 'app-fake-payment-modal',
  imports: [ModalShell],
  templateUrl: './fake-payment-modal.html',
  styleUrl: './fake-payment-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FakePaymentModal {
  private readonly fakePaymentModalService = inject(FakePaymentModalService);
  protected readonly state = this.fakePaymentModalService.state;

  protected respond(result: boolean): void {
    this.fakePaymentModalService.respond(result);
  }
}
