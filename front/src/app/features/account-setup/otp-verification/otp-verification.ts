import { Component, ElementRef, QueryList, ViewChildren, input, output, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountSetupData } from '../account-setup/account-setup';

@Component({
  selector: 'app-otp-verification',
  imports: [FormsModule],
  templateUrl: './otp-verification.html',
  styleUrls: ['../account-setup-shared.css', './otp-verification.css'],
})
export class OtpVerification {
  @ViewChildren('otpInput') inputEls!: QueryList<ElementRef<HTMLInputElement>>;

  readonly data = input<AccountSetupData>({});
  readonly verified = output<void>();
  readonly back = output<void>();

  protected readonly digits = signal<string[]>(['', '', '', '', '', '']);
  protected readonly isLoading = signal(false);
  protected readonly hasError = signal(false);
  protected readonly resendSent = signal(false);

  protected readonly otp = computed(() => this.digits().join(''));
  protected readonly isComplete = computed(() => /^\d{6}$/.test(this.otp()));

  protected updateDigit(index: number, value: string): void {
    const cleaned = value.replace(/\D/g, '').slice(-1);
    this.digits.update(d => { const arr = [...d]; arr[index] = cleaned; return arr; });
    this.hasError.set(false);
    if (cleaned && index < 5) this.focusAt(index + 1);
  }

  protected onKeyDown(e: KeyboardEvent, index: number): void {
    if (e.key === 'Backspace' && !this.digits()[index] && index > 0) {
      this.focusAt(index - 1);
    }
  }

  protected onPaste(e: ClipboardEvent): void {
    const text = e.clipboardData?.getData('text') ?? '';
    const nums = text.replace(/\D/g, '').slice(0, 6).split('');
    this.digits.set([...nums, ...Array(6 - nums.length).fill('')]);
    e.preventDefault();
    this.focusAt(Math.min(nums.length, 5));
  }

  protected async onVerify(): Promise<void> {
    if (!this.isComplete()) return;
    this.isLoading.set(true);
    this.hasError.set(false);
    await new Promise(r => setTimeout(r, 1200));
    this.isLoading.set(false);
    this.verified.emit();
  }

  protected onResend(): void {
    this.resendSent.set(true);
    setTimeout(() => this.resendSent.set(false), 4000);
  }

  private focusAt(index: number): void {
    const inputs = this.inputEls?.toArray();
    if (inputs?.[index]) inputs[index].nativeElement.focus();
  }
}
