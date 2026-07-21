import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';
import { SocialAccountsApiService } from '../../../core/api/social-accounts-api.service';
import { SchedulingApiService } from '../../../core/api/scheduling-api.service';
import { ContentApiService } from '../../../core/api/content-api.service';
import { MediaService } from '../../../services/media.service';
import { ApiError } from '../../../core/api';
import { SocialAccountSummary, SocialPlatform } from '../../../core/models';

const PLATFORM_CFG: Record<SocialPlatform, { icon: string; color: string; label: string }> = {
  Instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'إنستغرام' },
  Facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'فيسبوك'   },
  Tiktok:    { icon: 'fa-brands fa-tiktok',      color: '#222',    label: 'تيك توك'  },
  Youtube:   { icon: 'fa-brands fa-youtube',     color: '#FF0000', label: 'يوتيوب'  },
  Twitter:   { icon: 'fa-brands fa-x-twitter',   color: '#14171A', label: 'إكس'      },
  Linkedin:  { icon: 'fa-brands fa-linkedin-in', color: '#0A66C2', label: 'لينكد إن' },
};

@Component({
  selector: 'app-schedule-modal',
  imports: [RouterLink, ModalShell],
  templateUrl: './schedule-modal.html',
  styleUrls: ['../../dashboard/users/users-shared.css', './schedule-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleModal {
  readonly open = input(false);
  readonly contentItemId = input<string | null>(null);

  readonly closed = output<void>();
  readonly scheduled = output<void>();

  private readonly socialAccountsApi = inject(SocialAccountsApiService);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly contentApi = inject(ContentApiService);
  protected readonly media = inject(MediaService);

  protected readonly platformCfg = PLATFORM_CFG;

  protected readonly accounts = signal<SocialAccountSummary[]>([]);
  protected readonly loadingAccounts = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly selectedAccountId = signal<string | null>(null);
  protected readonly selectedVisualAssetId = signal<string>('');
  protected readonly scheduledAtLocal = signal('');

  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly visualAssetOptions = computed(() =>
    this.media.items().filter((i) => i.type === 'static-ad' && i.status === 'generated'),
  );

  protected readonly minDateTime = this.toLocalInputValue(new Date());

  constructor() {
    effect(() => {
      const contentItemId = this.contentItemId();
      if (this.open() && contentItemId) {
        this.resetForm();
        this.loadAccounts(contentItemId);
      }
    });
  }

  private resetForm(): void {
    this.selectedAccountId.set(null);
    this.selectedVisualAssetId.set('');
    this.scheduledAtLocal.set('');
    this.submitError.set(null);
  }

  /** Resolves accounts via the content item's own brand, not whatever brand happens to be
   * globally "active" - a content item may belong to a different brand than the one currently
   * selected elsewhere in the app, and scheduling must always target the right brand's accounts. */
  private loadAccounts(contentItemId: string): void {
    this.loadingAccounts.set(true);
    this.loadError.set(null);

    this.contentApi.getById(contentItemId).subscribe({
      next: (item) => {
        const brandProfileId = item.brandProfileId;
        if (!brandProfileId) {
          this.loadingAccounts.set(false);
          this.loadError.set('تعذر تحديد العلامة التجارية لهذا المحتوى.');
          return;
        }

        this.socialAccountsApi.getByBrand(brandProfileId).subscribe({
          next: (accounts) => {
            this.accounts.set(accounts.filter((a) => a.isActive));
            this.loadingAccounts.set(false);
          },
          error: (error: unknown) => {
            this.loadingAccounts.set(false);
            this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل الحسابات المرتبطة.');
          },
        });
      },
      error: (error: unknown) => {
        this.loadingAccounts.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'تعذر تحميل بيانات المحتوى.');
      },
    });
  }

  protected selectAccount(id: string): void {
    this.selectedAccountId.set(id);
  }

  protected setNow(): void {
    this.scheduledAtLocal.set(this.toLocalInputValue(new Date()));
  }

  protected submit(): void {
    const contentItemId = this.contentItemId();
    const socialAccountId = this.selectedAccountId();
    const scheduledAtLocal = this.scheduledAtLocal();

    if (!contentItemId || !socialAccountId || !scheduledAtLocal) {
      this.submitError.set('يرجى اختيار الحساب وموعد النشر.');
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.schedulingApi
      .schedule({
        contentItemId,
        visualAssetId: this.selectedVisualAssetId() || null,
        socialAccountId,
        scheduledAt: new Date(scheduledAtLocal).toISOString(),
        aiSuggestedTime: false,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.scheduled.emit();
          this.closed.emit();
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.submitError.set(error instanceof ApiError ? error.message : 'تعذر جدولة المنشور، حاول مرة أخرى.');
        },
      });
  }

  private toLocalInputValue(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
