import { Component, input, output, signal } from '@angular/core';
import { ScheduledPost, PostStatus } from '../../../model/scheduled-post.model';
import { SocialPlatform } from '../../../core/models';
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
  loadingDetail = input(false);
  close  = output<void>();
  cancelPost = output<string>();
  publishNow = output<string>();

  readonly showCancelConfirm = signal(false);

  readonly platformConfig: Record<SocialPlatform, { icon: string; color: string; label: string }> = {
    Instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
    Facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
    Tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#010101', label: 'تيك توك'  },
    Youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
    Twitter:   { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
    Linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
  };

  readonly statusOptions: { value: PostStatus; label: string; color: string }[] = [
    { value: 'Pending',   label: 'مجدول', color: '#3B82F6' },
    { value: 'Published', label: 'منشور', color: '#10B981' },
    { value: 'Cancelled', label: 'ملغى',  color: '#9CA3AF' },
    { value: 'Failed',    label: 'فشل',   color: '#EF4444' },
  ];

  get platform() { return this.platformConfig[this.post().platform]; }
  get statusInfo() { return this.statusOptions.find(s => s.value === this.post().status) ?? this.statusOptions[0]; }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  confirmCancel(): void { this.showCancelConfirm.set(true); }
  dismissCancelConfirm(): void { this.showCancelConfirm.set(false); }

  doCancel(): void { this.cancelPost.emit(this.post().id); }
  doPublishNow(): void { this.publishNow.emit(this.post().id); }
}
