import { Component, input, output } from '@angular/core';
import { Ad } from '../../../model/ad.model';

@Component({
  selector: 'app-ad-card',
  standalone: true,
  imports: [],
  templateUrl: './ad-card.html',
  styleUrl: './ad-card.css',
})
export class AdCard {
  readonly ad     = input.required<Ad>();
  readonly toggle = output<string>();
  readonly view   = output<string>();

  protected get platformIcon(): string {
    const m: Record<string, string> = {
      instagram: 'fa-brands fa-instagram',
      facebook:  'fa-brands fa-facebook-f',
      tiktok:    'fa-brands fa-tiktok',
      youtube:   'fa-brands fa-youtube',
      x:         'fa-brands fa-x-twitter',
      snapchat:  'fa-brands fa-snapchat',
      linkedin:  'fa-brands fa-linkedin-in',
    };
    return m[this.ad().platform] ?? 'fa-solid fa-globe';
  }

  protected get platformLabel(): string {
    const m: Record<string, string> = {
      instagram: 'إنستغرام', facebook: 'فيسبوك', tiktok: 'تيك توك',
      youtube: 'يوتيوب', x: 'إكس', snapchat: 'سناب شات', linkedin: 'لينكد إن',
    };
    return m[this.ad().platform] ?? this.ad().platform;
  }

  protected get statusLabel(): string {
    const m: Record<string, string> = {
      active: 'نشط', paused: 'موقوف', rejected: 'مرفوض', pending: 'قيد المراجعة', completed: 'مكتمل',
    };
    return m[this.ad().status] ?? '';
  }

  protected get formatLabel(): string {
    const m: Record<string, string> = {
      image: 'صورة', video: 'فيديو', carousel: 'كاروسيل', story: 'ستوري', reel: 'ريلز',
    };
    return m[this.ad().format] ?? '';
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
    return n.toString();
  }
}
