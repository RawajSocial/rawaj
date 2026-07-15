import { Component, computed, input, OnInit, output, signal } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-onboarding-step-seven',
  imports: [RevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-seven.html',
  styleUrl: './onboarding-step-seven.css',
})
export class OnboardingStepSeven implements OnInit {
  readonly currentStep = input(7);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly finish = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly questions = signal<Question[]>([
    {
      question: 'هل لديك منافسون محددون تريد التميز عنهم؟',
      suggestions: ['نعم، أعرفهم جيداً', 'السوق واسع ولم أحدد بعد', 'نعم لكن لم أحللهم'],
    },
    {
      question: 'ما أكبر تحدٍ واجهته في التسويق حتى الآن؟',
      suggestions: ['ضعف التفاعل', 'تحويل المتابعين لمبيعات', 'لم أبدأ بعد'],
    },
    {
      question: 'كيف يصل معظم عملائك الحاليين إليك؟',
      suggestions: ['توصيات شخصية', 'إنستغرام', 'إعلانات مدفوعة'],
    },
    {
      question: 'هل تستخدم مؤثرين في التسويق؟',
      suggestions: ['نعم وكانت ناجحة', 'جربت ولم تنجح', 'لا أعتمد على ذلك'],
    },
    {
      question: 'هل هناك أي تفاصيل إضافية أو شيء مهم لم نذكره؟',
      suggestions: ['لا، كل شيء واضح', 'نعم، لدي تفاصيل إضافية', 'غير متأكد'],
    },
  ]);

  protected readonly currentIndex = signal(0);
  protected readonly answers     = signal<Answer[]>([]);
  protected readonly inputValue  = signal('');
  protected readonly loading     = signal(false);

  protected readonly currentQuestion = computed(() => this.questions()[this.currentIndex()] ?? null);
  protected readonly done            = computed(() => this.currentIndex() >= this.questions().length);
  protected readonly totalQuestions  = computed(() => this.questions().length);

  // Personalized greeting using brand name from step 3
  protected readonly greeting = computed(() => {
    const name = this.data()?.brandName;
    return name
      ? `مرحباً بـ${name}! لدي بعض الأسئلة المخصصة التي ستساعدنا في بناء استراتيجيتك بدقة.`
      : 'أنا هنا لأسألك بعض الأسئلة المخصصة لمساعدتك في بناء استراتيجيتك.';
  });

  ngOnInit(): void {
    const saved = this.data()?.strategistAnswers ?? [];
    if (saved.length > 0) {
      this.answers.set(saved);
      const nextIdx = Math.min(saved.length, this.questions().length);
      this.currentIndex.set(nextIdx);
      if (nextIdx < this.questions().length) {
        this.simulateThinking();
      } else {
        this.loading.set(false);
      }
    } else {
      this.simulateThinking();
    }
  }

  protected updateInput(value: string): void {
    this.inputValue.set(value);
  }

  protected submitAnswer(value?: string): void {
    const answer = (value ?? this.inputValue()).trim();
    if (!answer || this.done()) return;

    const question = this.currentQuestion();
    if (!question) return;

    const nextAnswers = [...this.answers(), { question: question.question, answer }];
    this.answers.set(nextAnswers);
    this.dataChange.emit({ strategistAnswers: nextAnswers });
    this.inputValue.set('');
    this.currentIndex.update(idx => idx + 1);
    if (!this.done()) {
      this.simulateThinking();
    }
  }

  private simulateThinking(): void {
    this.loading.set(true);
    setTimeout(() => this.loading.set(false), 650);
  }

  protected onFinish(): void { this.finish.emit(); }
  protected onBack(): void   { this.back.emit(); }
}

type Question = { question: string; suggestions: string[] };
type Answer   = { question: string; answer: string };

type OnboardingData = {
  // From previous steps — used for personalization
  brandName?: string;
  sector?: string;
  positioningVs?: string;
  campaignOutcome?: string;
  successMetrics?: string[];
  // This step
  strategistAnswers?: Answer[];
};
