import { Component, computed } from '@angular/core';

interface RadarSeries {
  label: string;
  color: string;
  fill: string;
  values: number[];
}

@Component({
  selector: 'app-radar-chart',
  standalone: true,
  imports: [],
  templateUrl: './radar-chart.html',
  styleUrl: './radar-chart.css',
})
export class RadarChart {
  readonly axes = ['2018', '2019', '2020', '2021', '2022', '2023'];
  readonly maxValue = 100;
  readonly cx = 150;
  readonly cy = 150;
  readonly radius = 110;
  readonly rings = [25, 50, 75, 100];

  series: RadarSeries[] = [
    {
      label: 'المبيعات',
      color: '#2563EB',
      fill: 'rgba(37,99,235,0.18)',
      values: [80, 65, 90, 75, 85, 70],
    },
    {
      label: 'الإيرادات',
      color: '#7C3AED',
      fill: 'rgba(124,58,237,0.18)',
      values: [60, 80, 55, 90, 70, 95],
    },
    {
      label: 'النمو',
      color: '#FACC15',
      fill: 'rgba(250,204,21,0.18)',
      values: [45, 55, 70, 60, 80, 65],
    },
  ];

  ringPolygons = computed(() =>
    this.rings.map(pct => this.buildRingPath(pct))
  );

  axisLines = computed(() =>
    this.axes.map((_, i) => {
      const angle = this.axisAngle(i);
      return {
        x2: this.cx + this.radius * Math.cos(angle),
        y2: this.cy + this.radius * Math.sin(angle),
      };
    })
  );

  axisLabels = computed(() =>
    this.axes.map((label, i) => {
      const angle = this.axisAngle(i);
      const labelR = this.radius + 18;
      return {
        label,
        x: this.cx + labelR * Math.cos(angle),
        y: this.cy + labelR * Math.sin(angle),
      };
    })
  );

  seriesPaths = computed(() =>
    this.series.map(s => ({
      ...s,
      path: this.buildRadarPath(s.values, this.maxValue),
    }))
  );

  private axisAngle(i: number): number {
    return (Math.PI * 2 * i) / this.axes.length - Math.PI / 2;
  }

  private buildRingPath(pct: number): string {
    const r = (pct / 100) * this.radius;
    const points = this.axes.map((_, i) => {
      const angle = this.axisAngle(i);
      return `${this.cx + r * Math.cos(angle)},${this.cy + r * Math.sin(angle)}`;
    });
    return points.join(' ');
  }

  buildRadarPath(values: number[], maxValue: number): string {
    const points = values.map((v, i) => {
      const angle = this.axisAngle(i);
      const r = (v / maxValue) * this.radius;
      return `${this.cx + r * Math.cos(angle)},${this.cy + r * Math.sin(angle)}`;
    });
    return points.join(' ');
  }
}
