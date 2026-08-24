import { Component, HostListener, effect, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UpgradeTenantData } from '../../upgrade-tenant-page/upgrade-tenant-page';
import { ARAB_COUNTRIES, ArabCountry } from '../../upgrade-tenant.constants';

@Component({
  selector: 'app-agency-basic-info',
  imports: [ReactiveFormsModule],
  templateUrl: './agency-basic-info.html',
  styleUrls: ['../../upgrade-tenant-shared.css', './agency-basic-info.css'],
})
export class AgencyBasicInfo {
  readonly data = input<UpgradeTenantData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<UpgradeTenantData>>();

  protected readonly countries = ARAB_COUNTRIES;
  protected readonly selectedCountry = signal<ArabCountry>(ARAB_COUNTRIES[0]);
  protected readonly countryOpen = signal(false);

  protected readonly form = new FormGroup({
    agencyName:  new FormControl('', [Validators.required, Validators.minLength(2)]),
    agencyPhone: new FormControl('', [Validators.required, Validators.pattern(/^[0-9]{7,15}$/)]),
  });

  constructor() {
    effect(() => {
      const d = this.data();
      this.form.patchValue(
        { agencyName: d.agencyName ?? '', agencyPhone: d.agencyPhone ?? '' },
        { emitEvent: false }
      );
      if (d.agencyCountryCode) {
        const c = ARAB_COUNTRIES.find(x => x.code === d.agencyCountryCode);
        if (c) this.selectedCountry.set(c);
      }
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('.phone-country-dropdown')) {
      this.countryOpen.set(false);
    }
  }

  protected selectCountry(c: ArabCountry): void {
    this.selectedCountry.set(c);
    this.countryOpen.set(false);
  }

  protected onNext(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    this.dataChange.emit({
      agencyName: v.agencyName!,
      agencyPhone: v.agencyPhone!,
      agencyCountryCode: this.selectedCountry().code,
    });
    this.next.emit();
  }

  protected get nameCtrl()  { return this.form.get('agencyName')!; }
  protected get phoneCtrl() { return this.form.get('agencyPhone')!; }
}
