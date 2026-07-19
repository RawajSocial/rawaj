import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

export interface FaqItem {
  q: string;
  a: string;
}

@Component({
  selector: 'app-faq-section',
  imports: [],
  templateUrl: './faq-section.html',
  styleUrl: './faq-section.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FaqSection {
  readonly items = input.required<FaqItem[]>();

  protected readonly openIndex = signal<number | null>(0);

  protected toggle(i: number): void {
    this.openIndex.set(this.openIndex() === i ? null : i);
  }
}
