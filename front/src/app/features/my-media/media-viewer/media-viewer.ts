import { Component, HostListener, effect, inject, input, output, signal } from '@angular/core';
import {
  GeneratedItem, GenType,
  SIZE_CFG, TONE_CFG, TYPE_CFG,
} from '../../../model/generated-item.model';
import { MediaService } from '../../../services/media.service';
import { ScheduleModal } from '../schedule-modal/schedule-modal';
import { ContentRevisionSummary } from '../../../core/models';

@Component({
  selector: 'app-media-viewer',
  standalone: true,
  imports: [ScheduleModal],
  templateUrl: './media-viewer.html',
  styleUrl: './media-viewer.css',
})
export class MediaViewer {
  item = input.required<GeneratedItem>();

  readonly typeCfg = TYPE_CFG;
  readonly toneCfg = TONE_CFG;
  readonly sizeCfg = SIZE_CFG;

  closed = output<void>();

  readonly editMode          = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly scheduleModalOpen = signal(false);
  readonly currentItem       = signal<GeneratedItem | null>(null);

  readonly reviewing    = signal(false);
  readonly reviewError  = signal<string | null>(null);

  readonly regenerateFeedback = signal('');
  readonly regenerating       = signal(false);
  readonly regenerateError    = signal<string | null>(null);

  readonly revisionsOpen    = signal(false);
  readonly loadingRevisions = signal(false);
  readonly revisions        = signal<ContentRevisionSummary[]>([]);

  private readonly media = inject(MediaService);

  constructor() {
    effect(() => {
      this.currentItem.set(this.item());
      this.editMode.set(false);
      this.showDeleteConfirm.set(false);
      this.scheduleModalOpen.set(false);
      this.revisionsOpen.set(false);
      this.revisions.set([]);
      this.regenerateFeedback.set('');
      this.reviewError.set(null);
    });
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.scheduleModalOpen())  { return; } // ScheduleModal handles its own ESC via ModalShell
    if (this.editMode())          { this.editMode.set(false); return; }
    if (this.showDeleteConfirm()) { this.showDeleteConfirm.set(false); return; }
    this.closed.emit();
  }

  openSchedule(): void { this.scheduleModalOpen.set(true); }
  closeSchedule(): void { this.scheduleModalOpen.set(false); }

  reviewContent(approve: boolean): void {
    const item = this.currentItem();
    if (!item) return;

    this.reviewing.set(true);
    this.reviewError.set(null);

    this.media.reviewContent(item.id, approve).subscribe({
      next: (result) => {
        this.reviewing.set(false);
        this.currentItem.update((current) =>
          current ? { ...current, reviewStatus: result.status as GeneratedItem['reviewStatus'] } : current,
        );
      },
      error: () => {
        this.reviewing.set(false);
        this.reviewError.set('تعذر تحديث حالة المراجعة، حاول مرة أخرى.');
      },
    });
  }

  reviewVisualAsset(approve: boolean): void {
    const item = this.currentItem();
    if (!item) return;

    this.reviewing.set(true);
    this.reviewError.set(null);

    this.media.reviewVisualAsset(item.id, approve).subscribe({
      next: (result) => {
        this.reviewing.set(false);
        this.currentItem.update((current) => (current ? { ...current, isApproved: result.isApproved } : current));
      },
      error: () => {
        this.reviewing.set(false);
        this.reviewError.set('تعذر تحديث حالة المراجعة، حاول مرة أخرى.');
      },
    });
  }

  startEdit(): void {
    this.regenerateFeedback.set('');
    this.regenerateError.set(null);
    this.editMode.set(true);
  }

  cancelEdit(): void { this.editMode.set(false); }

  regenerate(): void {
    const item = this.currentItem();
    const feedback = this.regenerateFeedback().trim();
    if (!item || !feedback) {
      this.regenerateError.set('يرجى وصف التعديل المطلوب.');
      return;
    }

    this.regenerating.set(true);
    this.regenerateError.set(null);

    this.media.regenerate(item.id, feedback).subscribe({
      next: (result) => {
        this.regenerating.set(false);
        this.editMode.set(false);
        this.currentItem.update((current) =>
          current ? { ...current, textContent: result.content, description: result.content, reviewStatus: result.status } : current,
        );
      },
      error: (error: unknown) => {
        this.regenerating.set(false);
        this.regenerateError.set(
          error instanceof Error ? error.message : 'تعذر إعادة توليد المحتوى، حاول مرة أخرى.',
        );
      },
    });
  }

  toggleRevisions(): void {
    const item = this.currentItem();
    if (!item) return;

    const next = !this.revisionsOpen();
    this.revisionsOpen.set(next);
    if (next && this.revisions().length === 0) {
      this.loadingRevisions.set(true);
      this.media.getRevisions(item.id).subscribe({
        next: (revisions) => {
          this.revisions.set(revisions);
          this.loadingRevisions.set(false);
        },
        error: () => this.loadingRevisions.set(false),
      });
    }
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

  reviewStatusLabel(s?: string): string {
    return { Draft: 'مسودة', Reviewed: 'تمت المراجعة', Approved: 'معتمد', Rejected: 'مرفوض', Published: 'منشور' }[s ?? ''] ?? '';
  }

  reviewStatusColor(s?: string): string {
    return { Draft: '#9ca3af', Reviewed: '#3b82f6', Approved: '#22c55e', Rejected: '#ef4444', Published: '#7C3AED' }[s ?? ''] ?? '#9ca3af';
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('ar-SA', {
      year: 'numeric', month: 'numeric', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }
}
