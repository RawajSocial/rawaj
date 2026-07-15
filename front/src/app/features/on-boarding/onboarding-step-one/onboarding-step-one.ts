import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

type CampaignType = {
  id: string;
  title: string;
  description: string;
  icon: string;
  color: string;
};

@Component({
  selector: 'app-onboarding-step-one',
  imports: [CommonModule, RevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-one.html',
  styleUrl: './onboarding-step-one.css',
})
export class OnboardingStepOne {
  readonly currentStep = input(1);
  readonly totalSteps = input(7);
  readonly data = input<{ campaignType?: string } | null>(null);
  readonly next = output<void>();
  readonly dataChange = output<{ campaignType: string }>();

  protected readonly campaignTypes: CampaignType[] = [
    { id: 'new-business', title: 'إطلاق نشاط جديد', description: 'أطلق نشاطك وابنِ حضورك من الصفر', icon: 'fa-solid fa-rocket', color: '#7c3aed' },
    { id: 'new-product', title: 'إطلاق منتج جديد', description: 'قدّم منتجك بخطة تسويقية واضحة', icon: 'fa-solid fa-box-open', color: '#2563eb' },
    { id: 'drive-sales', title: 'زيادة المبيعات', description: 'حوّل المتابعين إلى عملاء وارفع معدلات البيع', icon: 'fa-solid fa-chart-line', color: '#16a34a' },
    { id: 'seasonal', title: 'حملة موسمية', description: 'استثمر المناسبات لتحقيق أثر أكبر', icon: 'fa-solid fa-calendar-days', color: '#d97706' },
    { id: 'leads', title: 'توليد عملاء', description: 'اجذب عملاء محتملين وحوّلهم لصفقات', icon: 'fa-solid fa-users', color: '#0891b2' },
    { id: 'awareness', title: 'الوعي بالعلامة', description: 'وسّع انتشار علامتك وعزّز حضورها في السوق', icon: 'fa-solid fa-bullhorn', color: '#e11d48' },
    { id: 'other', title: 'أخرى', description: 'حدد نوع حملتك بنفسك واشرح ما تريد تحقيقه', icon: 'fa-solid fa-ellipsis', color: '#64748b' },
  ];

  protected readonly selectedType = computed(() => this.data()?.campaignType ?? '');
  protected readonly canProceed = computed(() => !!this.selectedType());

  protected selectType(type: CampaignType): void {
    this.dataChange.emit({ campaignType: type.id });
  }

  protected onNext(): void {
    this.next.emit();
  }
}
