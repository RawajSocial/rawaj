import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-step-badge',
  imports: [CommonModule],
  templateUrl: './step-badge.html',
  styleUrl: './step-badge.css',
})
export class StepBadge {
  readonly text = input('');
  readonly icon = input('');
  readonly variant = input<'default' | 'ai'>('default');
}
