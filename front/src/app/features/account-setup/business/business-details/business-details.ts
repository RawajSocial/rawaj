import { Component, computed, effect, input, output, signal } from '@angular/core';
import { AccountSetupData } from '../../account-setup/account-setup';

const SIZES = [
  { label: 'منفرد',    value: 'solo',  icon: 'fa-solid fa-user' },
  { label: '٢–٥',     value: '2-5',   icon: 'fa-solid fa-user-group' },
  { label: '٦–١٥',    value: '6-15',  icon: 'fa-solid fa-users' },
  { label: '١٦–٥٠',  value: '16-50', icon: 'fa-solid fa-people-group' },
  { label: '٥٠+',    value: '50+',   icon: 'fa-solid fa-building-user' },
];

const HEAR_ABOUT = [
  { label: 'وسائل التواصل الاجتماعي', value: 'social',   icon: 'fa-solid fa-share-nodes' },
  { label: 'بحث جوجل',                value: 'google',   icon: 'fa-brands fa-google' },
  { label: 'توصية من صديق',           value: 'referral', icon: 'fa-solid fa-user-group' },
  { label: 'ITI',                     value: 'iti',      icon: 'fa-solid fa-graduation-cap' },
  { label: 'لينكد إن',                value: 'linkedin', icon: 'fa-brands fa-linkedin' },
  { label: 'أخرى',                    value: 'other',    icon: 'fa-solid fa-circle-dot' },
];

@Component({
  selector: 'app-business-details',
  imports: [],
  templateUrl: './business-details.html',
  styleUrls: ['../../account-setup-shared.css', './business-details.css'],
})
export class BusinessDetails {
  readonly data = input<AccountSetupData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<AccountSetupData>>();

  protected readonly sizes          = SIZES;
  protected readonly hearAboutList  = HEAR_ABOUT;

  protected readonly selectedSize    = signal('');
  protected readonly selectedHear    = signal('');
  protected readonly showError       = signal(false);
  protected readonly isValid = computed(() => !!this.selectedSize() && !!this.selectedHear());

  constructor() {
    effect(() => {
      const d = this.data();
      if (d.businessSize) this.selectedSize.set(d.businessSize);
      if (d.hearAboutUs)  this.selectedHear.set(d.hearAboutUs);
    });
  }

  protected onNext(): void {
    if (!this.isValid()) { this.showError.set(true); return; }
    this.dataChange.emit({ businessSize: this.selectedSize(), hearAboutUs: this.selectedHear() });
    this.next.emit();
  }
}
