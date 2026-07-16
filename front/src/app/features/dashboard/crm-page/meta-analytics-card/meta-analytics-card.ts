import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MetaWidget } from '../crm-page.model';

@Component({
  selector: 'app-meta-analytics-card',
  imports: [],
  templateUrl: './meta-analytics-card.html',
  styleUrls: ['../../dashboard-shared.css', './meta-analytics-card.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetaAnalyticsCard {
  readonly widgets = input.required<MetaWidget[]>();
}
