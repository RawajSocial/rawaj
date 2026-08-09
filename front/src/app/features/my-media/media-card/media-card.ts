import { Component, input, output } from '@angular/core';
import { GeneratedItem, GenType, TYPE_CFG } from '../../../model/generated-item.model';

@Component({
  selector: 'app-media-card',
  standalone: true,
  imports: [],
  templateUrl: './media-card.html',
  styleUrl: './media-card.css',
})
export class MediaCard {
  item = input.required<GeneratedItem>();

  readonly typeCfg = TYPE_CFG;

  cardClick = output<GeneratedItem>();

  isImage(item: GeneratedItem): boolean { return item.type === 'static-ad' && !!item.thumbnailUrl; }
  isVideo(item: GeneratedItem): boolean { return item.type === 'video'; }

  thumbnailGradient(type: GenType): string {
    return {
      'static-ad': 'linear-gradient(135deg,#fce4ec,#fce4ec66)',
      'video':     'linear-gradient(135deg,#fff3e0,#fff3e066)',
      'text':      'linear-gradient(135deg,#ede9fe,#ede9fe66)',
    }[type];
  }

  statusLabel(s: string): string {
    return { generating: 'يُنشأ الآن', generated: 'مكتمل', failed: 'فشل' }[s] ?? s;
  }

  statusColor(s: string): string {
    return { generating: '#f97316', generated: '#22c55e', failed: '#ef4444' }[s] ?? '#9ca3af';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
