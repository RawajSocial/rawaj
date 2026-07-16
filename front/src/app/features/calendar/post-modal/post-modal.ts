import { Component, input, output, signal } from '@angular/core';
import { ScheduledPost, PostStatus } from '../../../model/scheduled-post.model';
import { CampaignPlatform } from '../../../model/campaign.model';
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
  close  = output<void>();
  save   = output<ScheduledPost>();
  remove = output<string>();

  readonly editMode          = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly editContent       = signal('');
  readonly editDate          = signal('');
  readonly editTime          = signal('');
  readonly editStatus        = signal<PostStatus>('scheduled');
  readonly editHashtags      = signal('');

  readonly platformConfig: Record<CampaignPlatform, { icon: string; color: string; label: string }> = {
    instagram: { icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)', label: 'إنستغرام' },
    facebook:  { icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)',  label: 'فيسبوك'   },
    tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    label: 'تيك توك'  },
    youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)',   label: 'يوتيوب'  },
    x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)',         label: 'إكس'      },
    snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  label: 'سناب شات' },
    linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  label: 'لينكد إن' },
  };

  readonly statusOptions: { value: PostStatus; label: string; color: string }[] = [
    { value: 'scheduled', label: 'مجدول', color: '#3B82F6' },
    { value: 'published', label: 'منشور', color: '#10B981' },
    { value: 'draft',     label: 'مسودة', color: '#9CA3AF' },
    { value: 'failed',    label: 'فشل',   color: '#EF4444' },
  ];

  readonly mediaTypeLabels: Record<string, string> = {
    image: 'صورة', video: 'فيديو', carousel: 'كاروسيل', reel: 'ريل', story: 'ستوري',
  };

  get platform() { return this.platformConfig[this.post().platform]; }
  get statusInfo() { return this.statusOptions.find(s => s.value === this.post().status) ?? this.statusOptions[0]; }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  startEdit(): void {
    const p = this.post();
    this.editContent.set(p.content);
    this.editDate.set(p.scheduledAt.substring(0, 10));
    this.editTime.set(p.scheduledAt.substring(11, 16));
    this.editStatus.set(p.status);
    this.editHashtags.set((p.hashtags ?? []).join(' '));
    this.editMode.set(true);
    this.showDeleteConfirm.set(false);
  }

  cancelEdit(): void { this.editMode.set(false); this.showDeleteConfirm.set(false); }

  submitEdit(): void {
    const p = this.post();
    const hashtags = this.editHashtags().trim()
      ? this.editHashtags().trim().split(/\s+/)
      : undefined;
    this.save.emit({
      ...p,
      content: this.editContent(),
      scheduledAt: `${this.editDate()}T${this.editTime()}:00`,
      status: this.editStatus(),
      hashtags,
    });
  }

  confirmDelete(): void { this.remove.emit(this.post().id); }
}
