import { Component, effect, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AccountSetupData } from '../../account-setup/account-setup';

@Component({
  selector: 'app-agency-social-links',
  imports: [ReactiveFormsModule],
  templateUrl: './agency-social-links.html',
  styleUrls: ['../../account-setup-shared.css', './agency-social-links.css'],
})
export class AgencySocialLinks {
  readonly data = input<AccountSetupData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<AccountSetupData>>();

  protected readonly form = new FormGroup({
    website: new FormControl(''),
    facebook: new FormControl(''),
    instagram: new FormControl(''),
    youtube: new FormControl(''),
    tiktok: new FormControl(''),
    linkedin: new FormControl(''),
    x: new FormControl(''),
    snapchat: new FormControl(''),
  });

  constructor() {
    effect(() => {
      const d = this.data();
      this.form.patchValue({
        website: d.website ?? '',
        facebook: d.facebook ?? '',
        instagram: d.instagram ?? '',
        youtube: d.youtube ?? '',
        tiktok: d.tiktok ?? '',
        linkedin: d.linkedin ?? '',
        x: d.x ?? '',
        snapchat: d.snapchat ?? '',
      }, { emitEvent: false });
    });
  }

  protected onNext(): void {
    this.dataChange.emit(this.form.getRawValue() as Partial<AccountSetupData>);
    this.next.emit();
  }
}
