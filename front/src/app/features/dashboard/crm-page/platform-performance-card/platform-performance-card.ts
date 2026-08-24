import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PlatformStat, compactNumber } from '../crm-page.model';

@Component({
  selector: 'app-platform-performance-card',
  imports: [],
  templateUrl: './platform-performance-card.html',
  styleUrls: ['../../dashboard-shared.css', './platform-performance-card.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlatformPerformanceCard {
  readonly platforms = input.required<PlatformStat[]>();

  private readonly totalUniqueViewers = computed(() => this.platforms().reduce((a, p) => a + p.uniqueViewers, 0));

  protected uniqueViewersShare(p: PlatformStat): number {
    const total = this.totalUniqueViewers();
    return total > 0 ? Math.round((p.uniqueViewers / total) * 100) : 0;
  }

  protected readonly compact = compactNumber;
}
