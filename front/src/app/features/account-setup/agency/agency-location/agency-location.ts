import { Component, HostListener, effect, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountSetupData } from '../../account-setup/account-setup';
import { EGYPT_CITIES } from '../../account-setup.constants';

@Component({
  selector: 'app-agency-location',
  imports: [ReactiveFormsModule],
  templateUrl: './agency-location.html',
  styleUrls: ['../../account-setup-shared.css', './agency-location.css'],
})
export class AgencyLocation {
  readonly data = input<AccountSetupData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<AccountSetupData>>();

  protected readonly cities   = EGYPT_CITIES;
  protected readonly cityOpen = signal(false);

  protected readonly form = new FormGroup({
    country: new FormControl('مصر', [Validators.required]),
    city:    new FormControl('', [Validators.required]),
  });

  constructor() {
    effect(() => {
      const d = this.data();
      this.form.patchValue(
        { country: 'مصر', city: d.city ?? '' },
        { emitEvent: false }
      );
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('.setup-dropdown')) {
      this.cityOpen.set(false);
    }
  }

  protected selectCity(city: string): void {
    this.form.get('city')!.setValue(city);
    this.cityOpen.set(false);
  }

  protected onNext(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.dataChange.emit({ country: this.form.value.country!, city: this.form.value.city! });
    this.next.emit();
  }

  protected get cityCtrl() { return this.form.get('city')!; }
}
