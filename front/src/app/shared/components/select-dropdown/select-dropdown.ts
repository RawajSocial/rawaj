import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
}

/** Shared custom-styled dropdown for Reactive Forms — drop-in replacement for a native `<select>`
 *  via `formControlName`, styled consistently with `.rw-field` inputs instead of the browser's
 *  unstyleable native select. Reuse this anywhere a form needs a single-choice dropdown. */
@Component({
  selector: 'app-select-dropdown',
  imports: [],
  templateUrl: './select-dropdown.html',
  styleUrl: './select-dropdown.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'select-dropdown',
    '[class.open]': 'isOpen()',
    '[class.disabled]': 'disabled()',
    '(document:click)': 'onDocumentClick($event)',
  },
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectDropdown),
      multi: true,
    },
  ],
})
export class SelectDropdown implements ControlValueAccessor {
  readonly options = input.required<SelectOption[]>();
  readonly placeholder = input<string>('اختر...');

  protected readonly isOpen = signal(false);
  protected readonly value = signal<string | null>(null);
  protected readonly disabled = signal(false);

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  protected get selectedLabel(): string {
    return this.options().find(o => o.value === this.value())?.label ?? this.placeholder();
  }

  writeValue(value: string): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected toggleOpen(): void {
    if (this.disabled()) return;
    this.isOpen.update(v => !v);
    if (!this.isOpen()) this.onTouched();
  }

  protected selectOption(option: SelectOption): void {
    this.value.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.isOpen.set(false);
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.select-dropdown')) {
      this.isOpen.set(false);
    }
  }
}
