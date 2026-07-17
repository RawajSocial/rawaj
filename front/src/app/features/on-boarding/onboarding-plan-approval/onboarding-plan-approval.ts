import { Component, DestroyRef, inject, input, OnInit, output, signal, computed } from '@angular/core';

const PLATFORM_META: Record<string, { icon: string; color: string; label: string }> = {
  instagram: { icon: 'fa-brands fa-instagram',  color: '#E1306C', label: 'Instagram'   },
  facebook:  { icon: 'fa-brands fa-facebook-f', color: '#1877F2', label: 'Facebook'    },
  tiktok:    { icon: 'fa-brands fa-tiktok',     color: '#2d2d2d', label: 'TikTok'      },
  snapchat:  { icon: 'fa-brands fa-snapchat',   color: '#b8960c', label: 'Snapchat'    },
  twitter:   { icon: 'fa-brands fa-x-twitter',  color: '#14171A', label: 'X / Twitter' },
  youtube:   { icon: 'fa-brands fa-youtube',    color: '#FF0000', label: 'YouTube'     },
  linkedin:  { icon: 'fa-brands fa-linkedin',   color: '#0077B5', label: 'LinkedIn'    },
  whatsapp:  { icon: 'fa-brands fa-whatsapp',   color: '#25D366', label: 'WhatsApp'    },
};

const SUCCESS_METRIC_LABELS: Record<string, { label: string; icon: string }> = {
  followers: { label: 'متابعون أكثر',    icon: 'fa-solid fa-user-plus' },
  visits:    { label: 'زيارات الموقع',    icon: 'fa-solid fa-globe' },
  sales:     { label: 'مبيعات أكثر',      icon: 'fa-solid fa-arrow-trend-up' },
  leads:     { label: 'عملاء محتملون',    icon: 'fa-solid fa-bullseye' },
  downloads: { label: 'تحميلات أكثر',     icon: 'fa-solid fa-download' },
  enquiries: { label: 'استفسارات أكثر',   icon: 'fa-solid fa-envelope' },
  awareness: { label: 'وعي بالعلامة',     icon: 'fa-solid fa-bullhorn' },
};

@Component({
  selector: 'app-onboarding-plan-approval',
  imports: [],
  templateUrl: './onboarding-plan-approval.html',
  styleUrl: './onboarding-plan-approval.css',
})
export class OnboardingPlanApproval implements OnInit {
  readonly data    = input<ApprovalOnboardingData | null>(null);
  readonly approve = output<void>();
  readonly back    = output<void>();

  private readonly destroyRef = inject(DestroyRef);

  protected readonly phase       = signal<'loading' | 'summary'>('loading');
  protected readonly loadingStep = signal(0);

  protected readonly loadingMessages = [
    'مراجعة بيانات علامتك التجارية...',
    'تجهيز ملخص الحملة...',
  ];

  /** An honest summary of what the user actually entered - not an AI-generated plan. The real
   * AI strategy is generated after approval (see rawaj-onboarding.ts approvePlan()) and reviewed
   * on the Marketing Plan page once ready, since it can take a while and shouldn't block finishing
   * onboarding. */
  protected readonly summary = computed<OnboardingSummary>(() => {
    const d = this.data() ?? {};
    return {
      brandName: d.brandName || 'علامتك التجارية',
      sector: d.sector ?? '',
      goal: this.resolveGoal(d),
      duration: d.campaignDuration || null,
      budget: d.monthlyBudget || null,
      platforms: this.resolvePlatforms(d),
      successMetrics: (d.successMetrics ?? [])
        .map((m) => SUCCESS_METRIC_LABELS[m])
        .filter((m): m is { label: string; icon: string } => !!m),
      positioningVs: d.positioningVs || null,
    };
  });

  ngOnInit(): void {
    const total = this.loadingMessages.length;
    let current = 0;

    const tick = () => {
      current++;
      if (current < total) {
        this.loadingStep.set(current);
        const id = setTimeout(tick, 420);
        this.destroyRef.onDestroy(() => clearTimeout(id));
      } else {
        const id = setTimeout(() => this.phase.set('summary'), 420);
        this.destroyRef.onDestroy(() => clearTimeout(id));
      }
    };

    const id = setTimeout(tick, 420);
    this.destroyRef.onDestroy(() => clearTimeout(id));
  }

  private resolvePlatforms(d: ApprovalOnboardingData): PlatformMeta[] {
    const ranked   = d.platformRanking  ?? [];
    const audience = d.audiencePlatforms ?? [];
    const merged   = [...new Set([...ranked, ...audience])].slice(0, 6);
    const keys = merged.length ? merged : ['instagram', 'facebook'];
    return keys.map((key) => ({
      key,
      ...(PLATFORM_META[key] ?? { icon: 'fa-solid fa-hashtag', color: '#6b7280', label: key }),
    }));
  }

  private resolveGoal(d: ApprovalOnboardingData): string {
    if (d.campaignOutcome) return d.campaignOutcome.slice(0, 160);
    const map: Record<string, string> = {
      brand_awareness: 'بناء وعي قوي وانتشار واسع للعلامة التجارية',
      sales:           'تعزيز المبيعات وتحقيق نمو إيرادي ملموس',
      leads:           'توليد عملاء محتملين ذوي جودة عالية',
      engagement:      'تعزيز تفاعل الجمهور وبناء مجتمع متفاعل',
      launch:          'إطلاق ناجح يحقق أقصى تأثير في وقت قصير',
    };
    return map[d.campaignType ?? ''] ?? 'بناء حضور رقمي متميز يحقق أهدافك التجارية';
  }

  onApprove(): void { this.approve.emit(); }
  onBack(): void    { this.back.emit(); }
}

// ──── Types ────────────────────────────────────────────────────────────────
export type ApprovalOnboardingData = {
  brandName?: string;
  sector?: string;
  campaignType?: string;
  campaignDuration?: string;
  campaignOutcome?: string;
  gender?: 'female' | 'male' | 'all';
  audiencePlatforms?: string[];
  successMetrics?: string[];
  positioningVs?: string;
  monthlyBudget?: string;
  platformRanking?: string[];
};

type PlatformMeta = { key: string; icon: string; color: string; label: string };

type OnboardingSummary = {
  brandName: string;
  sector: string;
  goal: string;
  duration: string | null;
  budget: string | null;
  platforms: PlatformMeta[];
  successMetrics: { label: string; icon: string }[];
  positioningVs: string | null;
};
