import { Component, computed, input, output, signal } from '@angular/core';
import { DashboardChartPoint } from '../../../model/dashboard.model';
import { compactNumber } from '../crm-page/crm-page.model';

type FilterKey = '1Y' | '6M' | '1M' | 'ALL';

const FILTER_DAYS: Record<FilterKey, number> = { '1M': 30, '6M': 180, '1Y': 365, ALL: 730 };

@Component({
  selector: 'app-balance-chart',
  standalone: true,
  imports: [],
  templateUrl: './balance-chart.html',
  styleUrl: './balance-chart.css',
})
export class BalanceChart {
  /** Real time-series data from `DashboardService.charts()`. Empty until the dashboard loads. */
  readonly points = input<DashboardChartPoint[]>([]);
  /** Emits the number of days to request when the user picks a different range chip;
   *  the parent (CrmPage) re-calls `DashboardService.refresh(...)` with this value. */
  readonly daysChange = output<number>();

  activeFilter = signal<FilterKey>('1Y');

  readonly W = 800;
  readonly H = 250;
  readonly PAD = 20;

  private readonly reachSeries = computed(() => this.points().map(p => p.reach));
  private readonly impressionsSeries = computed(() => this.points().map(p => p.impressions));

  private readonly maxValue = computed(() => {
    const all = [...this.reachSeries(), ...this.impressionsSeries()];
    return all.length ? Math.max(...all, 1) : 1;
  });

  revenuePath = computed(() => this.buildPath(this.reachSeries()));
  revenueAreaPath = computed(() => this.buildAreaPath(this.reachSeries()));
  expensePath = computed(() => this.buildPath(this.impressionsSeries()));

  xLabels = computed(() => {
    const pts = this.points();
    if (pts.length < 2) return [];
    return pts.map((p, i) => ({
      label: new Date(p.date).toLocaleDateString('ar-SA', { day: 'numeric', month: 'short' }),
      x: this.PAD + (i / (pts.length - 1)) * (this.W - this.PAD * 2),
    }));
  });

  totalRevenue = computed(() => compactNumber(this.reachSeries().reduce((a, v) => a + v, 0)));
  totalExpenses = computed(() => compactNumber(this.impressionsSeries().reduce((a, v) => a + v, 0)));

  private scaleY(value: number): number {
    const max = this.maxValue();
    return this.H - this.PAD - ((value / max) * (this.H - this.PAD * 2));
  }

  private scaleX(index: number, total: number): number {
    return this.PAD + (index / (total - 1)) * (this.W - this.PAD * 2);
  }

  buildPath(data: number[]): string {
    if (data.length < 2) return '';
    const points = data.map((v, i) => ({ x: this.scaleX(i, data.length), y: this.scaleY(v) }));
    let d = `M ${points[0].x} ${points[0].y}`;
    for (let i = 1; i < points.length; i++) {
      const prev = points[i - 1];
      const curr = points[i];
      const cpx = (prev.x + curr.x) / 2;
      d += ` C ${cpx} ${prev.y} ${cpx} ${curr.y} ${curr.x} ${curr.y}`;
    }
    return d;
  }

  buildAreaPath(data: number[]): string {
    if (data.length < 2) return '';
    const linePath = this.buildPath(data);
    const lastX = this.scaleX(data.length - 1, data.length);
    const firstX = this.scaleX(0, data.length);
    const bottom = this.H - this.PAD;
    return `${linePath} L ${lastX} ${bottom} L ${firstX} ${bottom} Z`;
  }

  setFilter(f: FilterKey): void {
    this.activeFilter.set(f);
    this.daysChange.emit(FILTER_DAYS[f]);
  }
}
