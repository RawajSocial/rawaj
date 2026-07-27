import { Component, HostListener, effect, inject, input, output, signal } from '@angular/core';
import {
  ContentTone, GeneratedItem, GenType,
  SIZE_CFG, TONE_CFG, TYPE_CFG,
} from '../../../model/generated-item.model';
import { ContentItemService } from '../../../services/content-item.service';
import { VisualAssetService } from '../../../services/visual-asset.service';
import { ConfirmDialogService } from '../../../services/confirm-dialog.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

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

  readonly editMode    = signal(false);
  readonly deleting     = signal(false);
  readonly currentItem = signal<GeneratedItem | null>(null);

  readonly editBrand = signal('');
  readonly editDesc  = signal('');
  readonly editTone  = signal<ContentTone>('professional');

  private readonly contentItemService = inject(ContentItemService);
  private readonly visualAssetService = inject(VisualAssetService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly errorModalService = inject(ErrorModalService);

  constructor() {
    effect(() => {
      this.currentItem.set(this.item());
      this.editMode.set(false);
    });
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.editMode()) { this.editMode.set(false); return; }
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

  /** Updates this modal's own view only — there is no backend field for freely editing an
   *  already-generated item's brand/description/tone, so nothing here is persisted. */
  saveEdit(): void {
    const item = this.currentItem();
    if (!item) return;
    const updated: GeneratedItem = {
      ...item,
      brand:       this.editBrand(),
      description: this.editDesc() || undefined,
      tone:        this.editTone(),
    };
    this.currentItem.set(updated);
    this.editMode.set(false);
  }

  async confirmDelete(): Promise<void> {
    const item = this.currentItem();
    if (!item || !item.sourceKind) return;

    const confirmed = await this.confirmDialogService.confirm(
      'سيتم حذف هذا المحتوى نهائيًا ولا يمكن التراجع عن هذا الإجراء.',
      { title: 'حذف المحتوى', confirmLabel: 'حذف', variant: 'danger' },
    );
    if (!confirmed) return;

    this.deleting.set(true);
    const request = item.sourceKind === 'visual-asset'
      ? this.visualAssetService.delete(item.id)
      : this.contentItemService.delete(item.id);

    request.subscribe({
      next: () => {
        this.deleting.set(false);
        this.closed.emit();
      },
      error: err => {
        this.deleting.set(false);
        this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر حذف هذا المحتوى.'), { variant: 'error' });
      },
    });
  }

  /** Triggers a real browser download of the actual image/video file (not the whole-page
   *  navigation a bare `<a href>` would otherwise cause for a cross-origin Cloudinary URL). */
  async download(): Promise<void> {
    const item = this.currentItem();
    const url = item?.thumbnailUrl ?? item?.videoUrl;
    if (!item || !url) return;

    try {
      const response = await fetch(url);
      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      const extension = item.videoUrl ? 'mp4' : 'jpg';

      const link = document.createElement('a');
      link.href = objectUrl;
      link.download = `${item.title || 'media'}.${extension}`;
      link.click();
      URL.revokeObjectURL(objectUrl);
    } catch {
      this.errorModalService.show('تعذّر تنزيل الملف.', { variant: 'error' });
    }
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
