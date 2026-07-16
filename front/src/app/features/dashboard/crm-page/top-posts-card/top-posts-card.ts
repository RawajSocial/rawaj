import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TopPostView, compactNumber } from '../crm-page.model';

@Component({
  selector: 'app-top-posts-card',
  imports: [],
  templateUrl: './top-posts-card.html',
  styleUrls: ['../../dashboard-shared.css', './top-posts-card.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopPostsCard {
  readonly posts = input.required<TopPostView[]>();

  protected readonly compact = compactNumber;
}
