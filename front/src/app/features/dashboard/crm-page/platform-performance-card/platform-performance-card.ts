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

  private readonly totalReach = computed(() => this.platforms().reduce((a, p) => a + p.reach, 0));

  protected reachShare(p: PlatformStat): number {
    const total = this.totalReach();
    return total > 0 ? Math.round((p.reach / total) * 100) : 0;
  }

  protected readonly compact = compactNumber;
}
