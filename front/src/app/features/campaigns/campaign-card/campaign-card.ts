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
  readonly view = output<string>();

  protected get statusLabel(): string {
    const map: Record<string, string> = {
      Draft: 'مسودة', Active: 'نشطة', Paused: 'موقوفة', Completed: 'مكتملة', Archived: 'مؤرشفة',
    };
    return map[this.campaign().status] ?? this.campaign().status;
  }

  protected get platformIcons(): string[] {
    const iconMap: Record<string, string> = {
      Instagram: 'fa-brands fa-instagram',
      Facebook: 'fa-brands fa-facebook-f',
      Tiktok: 'fa-brands fa-tiktok',
      Youtube: 'fa-brands fa-youtube',
      Twitter: 'fa-brands fa-x-twitter',
      Linkedin: 'fa-brands fa-linkedin-in',
    };
    return this.campaign().platforms.map(p => iconMap[p] ?? 'fa-solid fa-globe');
  }
}
