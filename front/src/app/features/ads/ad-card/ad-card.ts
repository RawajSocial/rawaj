import { Component, input, output } from '@angular/core';
import { Ad } from '../../../model/ad.model';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

const POST_TYPE_LABELS: Record<string, string> = {
  text: 'منشور نصي', image: 'منشور بصورة', video: 'منشور فيديو',
  carousel: 'منشور كاروسيل', story: 'ستوري', reel: 'ريلز',
};

const PLATFORM_ICONS: Record<string, string> = {
  instagram: 'fa-brands fa-instagram',
  facebook:  'fa-brands fa-facebook-f',
};

const PLATFORM_COLORS: Record<string, string> = {
  instagram: 'var(--color-instagram)',
  facebook:  'var(--color-facebook)',
};

const PLATFORM_LABELS: Record<string, string> = {
  instagram: 'إنستغرام', facebook: 'فيسبوك',
};

@Component({
  selector: 'app-ad-card',
  standalone: true,
  imports: [TooltipDirective],
  templateUrl: './ad-card.html',
  styleUrl: './ad-card.css',
})
export class AdCard {
  readonly ad   = input.required<Ad>();
  readonly view = output<string>();

  /** Real image for 'image' posts, the static text-post.png for 'text'
   *  posts, or null (→ generic placeholder) for video/carousel/story/reel
   *  since we don't extract real video frames here. */
  protected get mediaSrc(): string | null {
    const a = this.ad();
    if (a.format === 'text') return '/text-post.png';
    if (a.format === 'image') return a.imageUrl ?? null;
    return null;
  }

  protected get postTypeLabel(): string {
    return POST_TYPE_LABELS[this.ad().format] ?? '';
  }

  protected platformIcon(p: string): string {
    return PLATFORM_ICONS[p] ?? 'fa-solid fa-globe';
  }

  protected platformColor(p: string): string {
    return PLATFORM_COLORS[p] ?? 'var(--color-text-muted)';
  }

  protected platformLabel(p: string): string {
    return PLATFORM_LABELS[p] ?? p;
  }

  protected get platformNames(): string {
    return this.ad().platforms.map(p => this.platformLabel(p)).join('، ');
  }

  protected get statusLabel(): string {
    const m: Record<string, string> = {
      active: 'نشط', paused: 'موقوف', rejected: 'مرفوض', pending: 'قيد المراجعة', completed: 'مكتمل',
    };
    return m[this.ad().status] ?? '';
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
    return n.toString();
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  protected formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit', hour12: true });
  }
}
