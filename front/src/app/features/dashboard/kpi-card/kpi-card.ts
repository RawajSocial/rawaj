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
  change = input(0);
  changeLabel = input('');
  accentColor = input('#2563EB');
}
