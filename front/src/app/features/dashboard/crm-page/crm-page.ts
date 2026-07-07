import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { KpiCard } from '../kpi-card/kpi-card';
import { BalanceChart } from '../balance-chart/balance-chart';
import { RadarChart } from '../radar-chart/radar-chart';

interface KpiData {
  title: string;
  value: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  change: number;
  accentColor: string;
}

@Component({
  selector: 'app-crm-page',
  standalone: true,
  imports: [RouterLink, KpiCard, BalanceChart, RadarChart],
  templateUrl: './crm-page.html',
  styleUrl: './crm-page.css',
})
export class CrmPage {
  kpis: KpiData[] = [
    {
      title: 'إجمالي العملاء',
      value: '28,541',
      icon: 'fa-users',
      iconBg: 'rgba(37,99,235,0.12)',
      iconColor: '#2563EB',
      change: 16.24,
      accentColor: '#2563EB',
    },
    {
      title: 'الصفقات المغلقة',
      value: '2,543',
      icon: 'fa-handshake',
      iconBg: 'rgba(250,204,21,0.15)',
      iconColor: '#CCA60F',
      change: 3.67,
      accentColor: '#FACC15',
    },
    {
      title: 'إجمالي الإيرادات',
      value: '$795.69k',
      icon: 'fa-dollar-sign',
      iconBg: 'rgba(22,163,74,0.12)',
      iconColor: '#16A34A',
      change: 7.55,
      accentColor: '#16A34A',
    },
    {
      title: 'معدل التحويل',
      value: '34.7%',
      icon: 'fa-chart-line',
      iconBg: 'rgba(220,38,38,0.10)',
      iconColor: '#DC2626',
      change: -2.34,
      accentColor: '#DC2626',
    },
  ];
}
