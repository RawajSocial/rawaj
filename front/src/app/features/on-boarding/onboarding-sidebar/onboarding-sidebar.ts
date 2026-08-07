import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';

type SidebarStep = {
  index: number;
  title: string;
  subtitle: string;
};

@Component({
  selector: 'app-onboarding-sidebar',
  imports: [CommonModule],
  templateUrl: './onboarding-sidebar.html',
  styleUrl: './onboarding-sidebar.css',
})
export class OnboardingSidebar {
  readonly currentStep = input(1);
  /** The wizard's only way out before finishing every step — see RawajOnboarding.confirmExit,
   *  which owns the confirm dialog and the navigation; this component just renders the trigger. */
  readonly exit = output<void>();

  protected readonly steps: SidebarStep[] = [
    { index: 1, title: 'نوع الحملة',         subtitle: 'اختر نوع الحملة المناسبة' },
    { index: 2, title: 'موجز الحملة',         subtitle: 'تفاصيل ومعلومات الحملة' },
    { index: 3, title: 'معلومات العلامة',     subtitle: 'الاسم، القطاع، الموقع' },
    { index: 4, title: 'المنتج والجمهور',     subtitle: 'ماذا تبيع ومن تستهدف' },
    { index: 5, title: 'الأهداف والميزانية', subtitle: 'النتائج المطلوبة والإنفاق' },
    { index: 6, title: 'الهوية والمرجعيات',  subtitle: 'الشخصية البصرية والأسلوب' },
    { index: 7, title: 'الاستراتيجية الذكية', subtitle: 'أسئلة مخصصة بالذكاء الاصطناعي' },
    { index: 8, title: 'مراجعة واعتماد الاستراتيجية', subtitle: 'راجع خطة رواج AI واعتمدها' },
  ];

  /** The wizard's own `currentStep` can run one past `totalSteps` (the review stage isn't counted
   *  in `totalSteps`, which only spans the answer-collection steps) — clamp so the bar never
   *  visually overflows past 100%. */
  protected readonly progressPercent = computed(() => {
    const total = this.steps.length;
    if (total <= 1) return 100;
    const step = Math.min(this.currentStep(), total);
    return ((step - 1) / (total - 1)) * 100;
  });

  protected isCompleted(stepIndex: number): boolean {
    return stepIndex < this.currentStep();
  }

  protected isActive(stepIndex: number): boolean {
    return stepIndex === this.currentStep();
  }
}
