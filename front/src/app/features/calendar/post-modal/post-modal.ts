import { Component, input, output, signal } from '@angular/core';
import { ScheduledPost, PostStatus } from '../../../model/scheduled-post.model';
import { CAMPAIGN_PLATFORM_META, CampaignPlatform } from '../../../model/campaign.model';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';

@Component({
  selector: 'app-post-modal',
  standalone: true,
  imports: [ModalShell],
  templateUrl: './post-modal.html',
  styleUrl: './post-modal.css',
})
export class PostModal {
  post   = input.required<ScheduledPost>();
  open   = input(true);
  /** Whether the signed-in user may reschedule/publish/delete — the calendar page owns the actual
   *  role check (PermissionService); this only controls what the modal offers. */
  canEdit = input(true);

  close       = output<void>();
  reschedule  = output<{ id: string; scheduledAt: string }>();
  publishNow  = output<string>();
  remove      = output<string>();

  readonly editMode          = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly editDate          = signal('');
  readonly editTime          = signal('');

  /** Shared across every surface that renders a platform badge — see CAMPAIGN_PLATFORM_META. */
  readonly platformConfig = CAMPAIGN_PLATFORM_META;

  readonly statusOptions: { value: PostStatus; label: string; color: string }[] = [
    { value: 'scheduled', label: 'مجدول', color: '#3B82F6' },
    { value: 'published', label: 'منشور', color: '#10B981' },
    { value: 'draft',     label: 'مسودة', color: '#9CA3AF' },
    { value: 'failed',    label: 'فشل',   color: '#EF4444' },
  ];

  get platform() { return this.platformConfig[this.post().platform]; }
  get statusInfo() { return this.statusOptions.find(s => s.value === this.post().status) ?? this.statusOptions[0]; }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-EG', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  /** Only the schedule time is editable here — post copy belongs to the ContentItem and is
   *  changed via regenerate (on the campaign content page), and status is server-derived from
   *  what actually happens when the post is published, not something to hand-set. */
  startEdit(): void {
    const p = this.post();
    this.editDate.set(p.scheduledAt.substring(0, 10));
    this.editTime.set(p.scheduledAt.substring(11, 16));
    this.editMode.set(true);
    this.showDeleteConfirm.set(false);
  }

  cancelEdit(): void { this.editMode.set(false); this.showDeleteConfirm.set(false); }

  submitReschedule(): void {
    this.reschedule.emit({ id: this.post().id, scheduledAt: `${this.editDate()}T${this.editTime()}:00` });
  }

  doPublishNow(): void {
    this.publishNow.emit(this.post().id);
  }

  confirmDelete(): void { this.remove.emit(this.post().id); }
}
