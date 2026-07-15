import { Component, HostListener, effect, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountSetupData } from '../../account-setup/account-setup';
import { ARAB_COUNTRIES, ArabCountry } from '../../account-setup.constants';

const INDUSTRIES = [
  { label: 'الأغذية والمشروبات',          icon: 'fa-solid fa-utensils' },
  { label: 'التجزئة والتجارة الإلكترونية', icon: 'fa-solid fa-bag-shopping' },
  { label: 'الأزياء والملابس',             icon: 'fa-solid fa-shirt' },
  { label: 'الجمال والعناية',              icon: 'fa-solid fa-spa' },
  { label: 'التكنولوجيا',                  icon: 'fa-solid fa-microchip' },
  { label: 'العقارات',                     icon: 'fa-solid fa-building' },
  { label: 'التعليم والتدريب',             icon: 'fa-solid fa-graduation-cap' },
  { label: 'الرعاية الصحية',              icon: 'fa-solid fa-heart-pulse' },
  { label: 'السياحة والسفر',               icon: 'fa-solid fa-plane' },
  { label: 'السيارات',                     icon: 'fa-solid fa-car' },
  { label: 'المال والبنوك',                icon: 'fa-solid fa-landmark' },
  { label: 'الضيافة والفنادق',             icon: 'fa-solid fa-hotel' },
  { label: 'البناء والتشييد',              icon: 'fa-solid fa-helmet-safety' },
  { label: 'الترفيه',                      icon: 'fa-solid fa-masks-theater' },
  { label: 'أخرى',                         icon: 'fa-solid fa-ellipsis' },
];

@Component({
  selector: 'app-business-basic-info',
  imports: [ReactiveFormsModule],
  templateUrl: './business-basic-info.html',
  styleUrls: ['../../account-setup-shared.css', './business-basic-info.css'],
})
export class BusinessBasicInfo {
  readonly data = input<AccountSetupData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<AccountSetupData>>();

  protected readonly industries     = INDUSTRIES;
  protected readonly countries      = ARAB_COUNTRIES;
  protected readonly selectedCountry = signal<ArabCountry>(ARAB_COUNTRIES[0]);
  protected readonly countryOpen    = signal(false);
  protected readonly industryOpen   = signal(false);

  protected readonly form = new FormGroup({
    businessName:  new FormControl('', [Validators.required, Validators.minLength(2)]),
    businessPhone: new FormControl('', [Validators.required, Validators.pattern(/^[0-9]{7,15}$/)]),
    industry:      new FormControl('', [Validators.required]),
  });

  constructor() {
    effect(() => {
      const d = this.data();
      this.form.patchValue({
        businessName:  d.businessName  ?? '',
        businessPhone: d.businessPhone ?? '',
        industry:      d.industry      ?? '',
      }, { emitEvent: false });
      if (d.businessCountryCode) {
        const c = ARAB_COUNTRIES.find(x => x.code === d.businessCountryCode);
        if (c) this.selectedCountry.set(c);
      }
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const target = e.target as HTMLElement;
    if (!target.closest('.phone-country-dropdown')) this.countryOpen.set(false);
    if (!target.closest('.setup-dropdown'))          this.industryOpen.set(false);
  }

  protected selectCountry(c: ArabCountry): void {
    this.selectedCountry.set(c);
    this.countryOpen.set(false);
  }

  protected selectIndustry(label: string): void {
    this.form.get('industry')!.setValue(label);
    this.industryOpen.set(false);
  }

  protected get selectedIndustryIcon(): string {
    return this.industries.find(i => i.label === this.industryCtrl.value)?.icon ?? 'fa-solid fa-briefcase';
  }

  protected onNext(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    this.dataChange.emit({
      businessName:        v.businessName!,
      businessPhone:       v.businessPhone!,
      businessCountryCode: this.selectedCountry().code,
      industry:            v.industry!,
    });
    this.next.emit();
  }

  protected get nameCtrl()     { return this.form.get('businessName')!; }
  protected get phoneCtrl()    { return this.form.get('businessPhone')!; }
  protected get industryCtrl() { return this.form.get('industry')!; }
}
