import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-step-heading',
  imports: [CommonModule],
  templateUrl: './step-heading.html',
  styleUrl: './step-heading.css',
})
export class StepHeading {
  readonly title = input('');
  readonly lead = input('');
}
