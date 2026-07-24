import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/components/page-header/page-header';
import { CampaignService } from '../../../services/campaign.service';
import { ScheduledPostService } from '../../../services/scheduled-post.service';
import { CampaignPlatform } from '../../../model/campaign.model';
import { PostStatus } from '../../../model/scheduled-post.model';
import { SeoService } from '../../../services/seo.service';
import { ErrorModalService } from '../../../services/error-modal.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

interface PlatformMeta {
  icon: string;
  color: string;
  label: string;
}

const PLATFORM_META: Record<CampaignPlatform, PlatformMeta> = {
  instagram: { icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)', label: 'إنستغرام' },
  facebook:  { icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)',  label: 'فيسبوك' },
  tiktok:    { icon: 'fa-brands fa-tiktok',      color: 'var(--color-tiktok)',    label: 'تيك توك' },
  youtube:   { icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)',   label: 'يوتيوب' },
  x:         { icon: 'fa-brands fa-x-twitter',   color: 'var(--color-x)',         label: 'إكس' },
  snapchat:  { icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)',  label: 'سناب شات' },
  linkedin:  { icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)',  label: 'لينكد إن' },
};

const STATUS_META: Record<PostStatus, { label: string; color: string }> = {
  scheduled: { label: 'مجدول', color: '#3B82F6' },
  published: { label: 'منشور', color: '#10B981' },
  draft:     { label: 'مسودة', color: '#9CA3AF' },
  failed:    { label: 'فشل',   color: '#EF4444' },
};

const MEDIA_TYPE_LABELS: Record<string, string> = {
  image: 'صورة', video: 'فيديو', carousel: 'كاروسيل', reel: 'ريل', story: 'ستوري',
};

@Component({
  selector: 'app-campaign-post-detail-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './campaign-post-detail-page.html',
  styleUrls: ['../../dashboard/dashboard-shared.css', './campaign-post-detail-page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CampaignPostDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campaignService = inject(CampaignService);
  private readonly scheduledPostService = inject(ScheduledPostService);
  private readonly seo = inject(SeoService);
  private readonly errorModalService = inject(ErrorModalService);

  protected readonly campaignId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly postId = this.route.snapshot.paramMap.get('postId') ?? '';

  protected readonly campaign = this.campaignService.getById(this.campaignId);
  protected readonly post = this.scheduledPostService.getById(this.postId);

  protected readonly platformMeta = PLATFORM_META;
  protected readonly statusMeta = STATUS_META;
  protected readonly mediaTypeLabels = MEDIA_TYPE_LABELS;

  constructor() {
    this.seo.setPageSeo({
      title: 'تفاصيل المنشور | رواج',
      description: 'بيانات المنشور المجدول: المنصة، المحتوى، والموعد.',
      keywords: 'رواج, تفاصيل المنشور, جدولة المنشورات',
      path: '/dashboard/campaigns/' + this.campaignId + '/posts/' + this.postId,
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }

  protected formatDateTime(iso: string): string {
    return new Date(iso).toLocaleDateString('ar-SA', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: true,
    });
  }

  protected deletePost(): void {
    this.scheduledPostService.cancel(this.postId).subscribe({
      next: () => {
        this.scheduledPostService.remove(this.postId);
        this.router.navigate(['/dashboard/campaigns', this.campaignId, 'calendar']);
      },
      error: err => this.errorModalService.show(extractApiErrorMessage(err, 'تعذّر إلغاء جدولة المنشور.'), { variant: 'error' }),
    });
  }
}
