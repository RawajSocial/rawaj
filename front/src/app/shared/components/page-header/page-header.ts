import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Breadcrumb, Crumb } from '../breadcrumb/breadcrumb';

export type { Crumb };

@Component({
  selector: 'app-page-header',
  imports: [Breadcrumb],
  templateUrl: './page-header.html',
  styleUrl: './page-header.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly breadcrumbs = input<Crumb[]>([]);
  readonly subtitle = input<string>('');
}
