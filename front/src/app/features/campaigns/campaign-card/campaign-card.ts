import { Component, input, output } from '@angular/core';
import { Campaign } from '../../../model/campaign.model';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

const DEFAULT_LOGO = '/assets/icons/logo.png';
const RING_RADIUS = 42;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

@Component({
  selector: 'app-campaign-card',
  standalone: true,
  imports: [TooltipDirective],
  templateUrl: './campaign-card.html',
  styleUrl: './campaign-card.css',
})
export class CampaignCard {
  readonly campaign = input.required<Campaign>();
  readonly pause    = output<string>();
  readonly resume   = output<string>();
  readonly view     = output<string>();

  protected readonly ringRadius = RING_RADIUS;
  protected readonly ringCircumference = RING_CIRCUMFERENCE;

  /** Per-campaign brand logo shown on the banner — falls back to the Rawaj
   *  logo when a campaign doesn't set its own (see CampaignService). */
  protected get logoUrl(): string {
    return this.campaign().logoUrl ?? DEFAULT_LOGO;
  }

  protected get progressPct(): number {
    const c = this.campaign();
    return c.budget > 0 ? Math.min(100, Math.round((c.spent / c.budget) * 100)) : 0;
  }

  protected get ringDashOffset(): number {
    return RING_CIRCUMFERENCE * (1 - this.progressPct / 100);
  }

  protected get remainingBudget(): number {
    return Math.max(0, this.campaign().budget - this.campaign().spent);
  }

  protected get statusLabel(): string {
    const map: Record<string, string> = {
      active: 'نشطة', paused: 'موقوفة', completed: 'مكتملة', draft: 'مسودة',
    };
    return map[this.campaign().status] ?? '';
  }

  protected get statusIcon(): string {
    const map: Record<string, string> = {
      active: 'fa-solid fa-bullhorn', paused: 'fa-solid fa-pause', completed: 'fa-solid fa-circle-check', draft: 'fa-solid fa-pen',
    };
    return map[this.campaign().status] ?? 'fa-solid fa-circle';
  }

  protected get objectiveLabel(): string {
    const map: Record<string, string> = {
      awareness: 'الوعي بالعلامة', traffic: 'زيارات الموقع',
      engagement: 'التفاعل', leads: 'توليد عملاء', sales: 'رفع المبيعات',
    };
    return map[this.campaign().objective] ?? '';
  }

  protected get platformIcons(): { key: string; icon: string; color: string }[] {
    const map: Record<string, { icon: string; color: string }> = {
      instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)' },
      facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)' },
      tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)' },
      youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)' },
      x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)' },
      snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)' },
      linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)' },
    };
    return this.campaign().platforms.map(p => ({
      key: p,
      ...(map[p] ?? { icon: 'fa-solid fa-globe', color: 'var(--color-text-muted)' }),
    }));
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
    return n.toString();
  }
}
