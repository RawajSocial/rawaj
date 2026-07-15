import { Component, computed, effect, input, output, signal } from '@angular/core';
import { AccountSetupData } from '../../account-setup/account-setup';

const SIZES        = ['١–٥', '٦–١٥', '١٦–٥٠', '٥٠+'];
const SIZE_VALUES  = ['1-5', '6-15', '16-50', '50+'];
const CLIENT_RANGES       = ['١–٥', '٦–١٥', '١٦–٣٠', '٣٠+'];
const CLIENT_RANGE_VALUES = ['1-5', '6-15', '16-30', '30+'];

@Component({
  selector: 'app-agency-size-clients',
  imports: [],
  templateUrl: './agency-size-clients.html',
  styleUrls: ['../../account-setup-shared.css', './agency-size-clients.css'],
})
export class AgencySizeClients {
  readonly data = input<AccountSetupData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<AccountSetupData>>();

  protected readonly sizes        = SIZES;
  protected readonly sizeValues   = SIZE_VALUES;
  protected readonly clientRanges = CLIENT_RANGES;
  protected readonly clientValues = CLIENT_RANGE_VALUES;

  protected readonly selectedSize    = signal('');
  protected readonly selectedClients = signal('');
  protected readonly showError       = signal(false);

  protected readonly isValid = computed(() => !!this.selectedSize() && !!this.selectedClients());

  constructor() {
    effect(() => {
      const d = this.data();
      if (d.agencySize)    this.selectedSize.set(d.agencySize);
      if (d.activeClients) this.selectedClients.set(d.activeClients);
    });
  }

  protected onNext(): void {
    if (!this.isValid()) { this.showError.set(true); return; }
    this.dataChange.emit({ agencySize: this.selectedSize(), activeClients: this.selectedClients() });
    this.next.emit();
  }
}
