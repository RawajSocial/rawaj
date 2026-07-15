import { Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [],
  templateUrl: './kpi-card.html',
  styleUrl: './kpi-card.css',
})
export class KpiCard {
  title = input('');
  value = input('');
  icon = input('');
  iconBg = input('');
  iconColor = input('');
  change = input<number | null>(null);
  accentColor = input('#2563EB');
}
