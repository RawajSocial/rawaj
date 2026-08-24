import { Component, output } from '@angular/core';

@Component({
  selector: 'app-mp-empty-state',
  standalone: true,
  imports: [],
  templateUrl: './mp-empty-state.html',
  styleUrl: './mp-empty-state.css',
})
export class MpEmptyState {
  startPlan = output<void>();
}
