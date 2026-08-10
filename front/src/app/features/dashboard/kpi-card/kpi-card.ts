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
  /** null when there's no prior-period value to compare against yet — renders as "—", never "0%". */
  change = input<number | null>(0);
  changeLabel = input('');
  accentColor = input('#2563EB');
}
