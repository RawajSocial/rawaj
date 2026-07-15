import { Component, HostListener, effect, inject, input, output, signal } from '@angular/core';
import {
  ContentTone, GeneratedItem, GenType,
  SIZE_CFG, TONE_CFG, TYPE_CFG,
} from '../../../model/generated-item.model';
import { MediaService } from '../../../services/media.service';

@Component({
  selector: 'app-media-viewer',
  standalone: true,
  imports: [],
  templateUrl: './media-viewer.html',
  styleUrl: './media-viewer.css',
})
export class MediaViewer {
  item = input.required<GeneratedItem>();

  readonly typeCfg = TYPE_CFG;
  readonly toneCfg = TONE_CFG;
  readonly sizeCfg = SIZE_CFG;
  readonly tones: ContentTone[] = ['professional', 'casual', 'energetic', 'luxurious'];

  closed = output<void>();

  readonly editMode          = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly currentItem       = signal<GeneratedItem | null>(null);

  readonly editBrand = signal('');
  readonly editDesc  = signal('');
  readonly editTone  = signal<ContentTone>('professional');

  private readonly media = inject(MediaService);

  constructor() {
    effect(() => {
      this.currentItem.set(this.item());
      this.editMode.set(false);
      this.showDeleteConfirm.set(false);
    });
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.editMode())          { this.editMode.set(false); return; }
    if (this.showDeleteConfirm()) { this.showDeleteConfirm.set(false); return; }
    this.closed.emit();
  }

  startEdit(): void {
    const item = this.currentItem();
    if (!item) return;
    this.editBrand.set(item.brand);
    this.editDesc.set(item.description ?? '');
    this.editTone.set(item.tone ?? 'professional');
    this.editMode.set(true);
  }

  cancelEdit(): void { this.editMode.set(false); }

  saveEdit(): void {
    const item = this.currentItem();
    if (!item) return;
    const updated: GeneratedItem = {
      ...item,
      brand:       this.editBrand(),
      description: this.editDesc() || undefined,
      tone:        this.editTone(),
    };
    this.media.update(updated);
    this.currentItem.set(updated);
    this.editMode.set(false);
  }

  confirmDelete(): void { this.showDeleteConfirm.set(true); }
  cancelDelete(): void  { this.showDeleteConfirm.set(false); }

  deleteItem(): void {
    const item = this.currentItem();
    if (!item) return;
    this.media.remove(item.id);
    this.closed.emit();
  }

  isVideo(item: GeneratedItem): boolean { return item.type === 'video'; }
  isImage(item: GeneratedItem): boolean { return item.type === 'static-ad' && !!item.thumbnailUrl; }

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

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('ar-SA', {
      year: 'numeric', month: 'numeric', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }
}
