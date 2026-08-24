import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Shared cost caption for any coin-spending button — reproduces the exact wording pattern already
 * used ad hoc in content-gen-page.ts (`spendPreviewLabel`), centralized so every campaign-path
 * action states its cost identically. Always read `discountedCosts` from CoinPricingService, never
 * `baseCosts`, when passing `cost` in.
 */
@Component({
  selector: 'app-coin-cost-hint',
  imports: [],
  templateUrl: './coin-cost-hint.html',
  styleUrl: './coin-cost-hint.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CoinCostHint {
  /** Discounted per-unit cost. */
  readonly cost = input.required<number>();
  /** How many units this action spends per click (e.g. scheduling: 1 per approved post). */
  readonly multiplier = input<number>(1);
  /** Free-trial quota remaining, if this action has one (e.g. first N content generations). */
  readonly freeRemaining = input<number | null>(null);
  /** This specific call is free regardless of quota (e.g. a tenant's first marketing plan). */
  readonly isFree = input<boolean>(false);
  /** Coins are only charged on success (e.g. competitor research, which skips the charge when
   *  no data is found) — wording says so instead of implying a charge always happens. */
  readonly conditional = input<boolean>(false);

  protected readonly label = computed(() => {
    if (this.isFree()) return 'مجاني لهذه المرة';

    const free = this.freeRemaining();
    if (free !== null && free > 0) {
      return `توليد مجاني ضمن باقتك التجريبية (متبقي ${free})`;
    }

    const total = this.cost() * this.multiplier();
    const amount = total.toLocaleString('ar-EG');
    const prefix = this.multiplier() > 1
      ? `حتى ${amount} كوين (${this.cost().toLocaleString('ar-EG')} × ${this.multiplier()})`
      : `${amount} كوين`;

    return this.conditional()
      ? `يُخصم ${prefix} فقط عند النجاح`
      : `سيتم خصم ${prefix} من رصيدك`;
  });
}
