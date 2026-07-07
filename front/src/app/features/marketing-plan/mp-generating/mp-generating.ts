import { Component, input } from '@angular/core';
import { CalendarPost, Stage } from '../marketing-plan-page/marketing-plan-page';

@Component({
  selector: 'app-mp-generating',
  standalone: true,
  imports: [],
  templateUrl: './mp-generating.html',
  styleUrl: './mp-generating.css',
})
export class MpGenerating {
  stages        = input.required<Stage[]>();
  completedCount = input.required<number>();
  progressPct   = input.required<number>();
  streamedItems = input.required<CalendarPost[]>();
}
