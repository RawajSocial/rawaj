import { Component, input, output } from '@angular/core';
import { Campaign } from '../../../model/campaign.model';

@Component({
  selector: 'app-campaign-card',
  standalone: true,
  imports: [],
  templateUrl: './campaign-card.html',
  styleUrl: './campaign-card.css',
})
export class CampaignCard {
  readonly campaign = input.required<Campaign>();
  readonly pause    = output<string>();
  readonly resume   = output<string>();
  readonly view     = output<string>();

  protected get progressPct(): number {
    const c = this.campaign();
    return c.budget > 0 ? Math.min(100, Math.round((c.spent / c.budget) * 100)) : 0;
  }

  protected get statusLabel(): string {
    const map: Record<string, string> = {
      active: 'نشطة', paused: 'موقوفة', completed: 'مكتملة', draft: 'مسودة',
    };
    return map[this.campaign().status] ?? '';
  }

  protected get objectiveLabel(): string {
    const map: Record<string, string> = {
      awareness: 'الوعي بالعلامة', traffic: 'زيارات الموقع',
      engagement: 'التفاعل', leads: 'توليد عملاء', sales: 'رفع المبيعات',
    };
    return map[this.campaign().objective] ?? '';
  }

  protected get platformIcons(): string[] {
    const iconMap: Record<string, string> = {
      instagram: 'fa-brands fa-instagram',
      facebook:  'fa-brands fa-facebook-f',
      tiktok:    'fa-brands fa-tiktok',
      youtube:   'fa-brands fa-youtube',
      x:         'fa-brands fa-x-twitter',
      snapchat:  'fa-brands fa-snapchat',
      linkedin:  'fa-brands fa-linkedin-in',
    };
    return this.campaign().platforms.map(p => iconMap[p] ?? 'fa-solid fa-globe');
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
    return n.toString();
  }
}
