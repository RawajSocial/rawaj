import { Component, computed, signal } from '@angular/core';

type FilterKey = '1Y' | '6M' | '1M' | 'ALL';

interface ChartDataset {
  revenue: number[];
  expenses: number[];
  labels: string[];
}

@Component({
  selector: 'app-balance-chart',
  standalone: true,
  imports: [],
  templateUrl: './balance-chart.html',
  styleUrl: './balance-chart.css',
})
export class BalanceChart {
  activeFilter = signal<FilterKey>('1Y');

  private readonly allData: Record<FilterKey, ChartDataset> = {
    '1Y': {
      labels: ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'],
      revenue: [55, 70, 60, 80, 72, 90, 85, 95, 78, 88, 92, 100],
      expenses: [40, 45, 50, 42, 55, 48, 60, 52, 58, 50, 62, 55],
    },
    '6M': {
      labels: ['يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'],
      revenue: [85, 95, 78, 88, 92, 100],
      expenses: [60, 52, 58, 50, 62, 55],
    },
    '1M': {
      labels: ['الأسبوع 1', 'الأسبوع 2', 'الأسبوع 3', 'الأسبوع 4'],
      revenue: [88, 94, 90, 100],
      expenses: [55, 60, 58, 62],
    },
    'ALL': {
      labels: ['2020', '2021', '2022', '2023', '2024', '2025'],
      revenue: [40, 55, 65, 75, 88, 100],
      expenses: [30, 38, 45, 50, 56, 60],
    },
  };

  currentData = computed(() => this.allData[this.activeFilter()]);

  readonly W = 800;
  readonly H = 250;
  readonly PAD = 20;

  revenuePath = computed(() => this.buildPath(this.currentData().revenue));
  revenueAreaPath = computed(() => this.buildAreaPath(this.currentData().revenue));
  expensePath = computed(() => this.buildPath(this.currentData().expenses));

  xLabels = computed(() => {
    const labels = this.currentData().labels;
    return labels.map((label, i) => ({
      label,
      x: this.PAD + (i / (labels.length - 1)) * (this.W - this.PAD * 2),
    }));
  });

  private scaleY(value: number): number {
    const max = 120;
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
  }

  totalRevenue = '795.69 ألف $';
  totalExpenses = '415.37 ألف $';
}
