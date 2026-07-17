import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';

export interface AddCompetitorValue {
  name: string;
  url: string | null;
}

@Component({
  selector: 'app-add-competitor-modal',
  imports: [ReactiveFormsModule, ModalShell],
  templateUrl: './add-competitor-modal.html',
  styleUrls: ['../../dashboard/users/users-shared.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddCompetitorModal {
  readonly open = input(false);
  readonly submitting = input(false);
  readonly serverError = input<string | null>(null);

  readonly closed = output<void>();
  readonly save = output<AddCompetitorValue>();

  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    url: [''],
  });

  protected readonly submitted = signal(false);

  protected requestClose(): void {
    this.form.reset({ name: '', url: '' });
    this.submitted.set(false);
    this.closed.emit();
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, url } = this.form.getRawValue();
    this.save.emit({ name: name.trim(), url: url.trim() || null });
  }
}
