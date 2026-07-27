import { Component, inject, input, output } from '@angular/core';
import {
  CAMPAIGN_PLATFORM_META, CAMPAIGN_STATUS_LABELS, Campaign, CampaignStatus, campaignObjectiveLabel,
} from '../../../model/campaign.model';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';
import { PermissionService } from '../../../core/tenant/permission.service';
import { formatCampaignDateRange } from '../campaign-format.util';

const DEFAULT_LOGO = '/assets/icons/logo.png';

const STATUS_ICONS: Record<CampaignStatus, string> = {
  active: 'fa-solid fa-bullhorn', paused: 'fa-solid fa-pause', completed: 'fa-solid fa-circle-check',
  draft: 'fa-solid fa-pen', archived: 'fa-solid fa-box-archive',
};

@Component({
  selector: 'app-campaign-card',
  standalone: true,
  imports: [TooltipDirective],
  templateUrl: './campaign-card.html',
  styleUrl: './campaign-card.css',
})
export class CampaignCard {
  protected readonly perms = inject(PermissionService);

  readonly campaign = input.required<Campaign>();
  readonly pause    = output<string>();
  readonly resume   = output<string>();
  readonly view     = output<string>();
  /** Archive is the product's soft delete; restore brings one back. Both are reversible, so the
   *  card offers whichever applies to this campaign's current status. */
  readonly archive  = output<string>();
  readonly restore  = output<string>();
  /** "Continue where you left off" — routes to the strategy review or the content review
   *  depending on how far the campaign has actually got. */
  readonly openNextStep = output<string>();

  /** Per-campaign brand logo shown on the banner — falls back to the Rawaj
   *  logo when a campaign doesn't set its own (see CampaignService). */
  protected get logoUrl(): string {
    return this.campaign().logoUrl ?? DEFAULT_LOGO;
  }

  protected get budgetLabel(): string {
    const c = this.campaign();
    if (!c.budget) return '—';
    return `${this.formatNumber(c.budget)} ${c.budgetCurrency ?? ''}`.trim();
  }

  /** The campaign's lifecycle stage, which drives the card's primary CTA. Spend/performance
   *  metrics aren't shown on the card: nothing in the campaigns list API carries them, and it
   *  used to render a hardcoded 0 for CTR/clicks/reach on every campaign. Real per-campaign
   *  performance lives on the detail page, which reads it from AnalyticsService. */
  protected get stage(): 'needs-strategy' | 'needs-content' | 'has-content' {
    const c = this.campaign();
    if (!c.planApprovedAt) return 'needs-strategy';
    return c.contentItemCount > 0 ? 'has-content' : 'needs-content';
  }

  protected get nextStepLabel(): string {
    switch (this.stage) {
      case 'needs-strategy': return 'مراجعة الاستراتيجية';
      case 'needs-content':  return 'توليد المحتوى';
      default:               return 'مراجعة المحتوى';
    }
  }

  protected get nextStepIcon(): string {
    switch (this.stage) {
      case 'needs-strategy': return 'fa-solid fa-lightbulb';
      case 'needs-content':  return 'fa-solid fa-wand-magic-sparkles';
      default:               return 'fa-regular fa-images';
    }
  }

  protected get statusLabel(): string {
    return CAMPAIGN_STATUS_LABELS[this.campaign().status] ?? '';
  }

  protected get statusIcon(): string {
    return STATUS_ICONS[this.campaign().status] ?? 'fa-solid fa-circle';
  }

  protected get objectiveLabel(): string {
    return campaignObjectiveLabel(this.campaign().objective);
  }

  /** The campaign's own dates, formatted — the raw ISO strings used to be printed straight into
   *  the footer, and a campaign with no dates rendered a bare "—" separator with nothing on
   *  either side of it. */
  protected get dateRangeLabel(): string {
    const c = this.campaign();
    return formatCampaignDateRange(c.startDate, c.endDate);
  }

  protected get platformIcons(): { key: string; icon: string; color: string }[] {
    return this.campaign().platforms.map(p => ({
      key: p,
      ...(CAMPAIGN_PLATFORM_META[p] ?? { icon: 'fa-solid fa-globe', color: 'var(--color-text-muted)' }),
    }));
  }

  protected formatNumber(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K';
    return n.toString();
  }
}
