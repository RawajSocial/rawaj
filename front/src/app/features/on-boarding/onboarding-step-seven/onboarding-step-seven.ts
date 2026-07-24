import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import { BrandProfileService } from '../../../services/brand-profile.service';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

@Component({
  selector: 'app-onboarding-step-seven',
  imports: [GsapRevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-seven.html',
  styleUrl: './onboarding-step-seven.css',
})
export class OnboardingStepSeven implements OnInit {
  readonly currentStep = input(7);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly brandProfileId = input<string | null>(null);
  /** True while the parent is creating the campaign after `finish` fires — keeps the button
   *  showing a busy state instead of letting the user double-submit. */
  readonly submitting = input(false);
  readonly finish = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  private readonly brandProfileService = inject(BrandProfileService);

  protected readonly questions = signal<Question[]>([]);
  protected readonly loadError = signal<string | null>(null);

  protected readonly currentIndex = signal(0);
  protected readonly answers     = signal<Answer[]>([]);
  protected readonly inputValue  = signal('');
  protected readonly loading     = signal(true);

  protected readonly currentQuestion = computed(() => this.questions()[this.currentIndex()] ?? null);
  protected readonly done            = computed(() => this.questions().length > 0 && this.currentIndex() >= this.questions().length);
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
    }
    this.loadQuestions(saved.length);
  }

  /** Calls the real AI follow-up question generator (charges AI Reasoning Conversation coins),
   *  passing everything collected in the wizard so far as context. `answeredCount` resumes past
   *  whatever was already answered in a previous visit to this step. */
  private loadQuestions(answeredCount: number): void {
    const brandProfileId = this.brandProfileId();
    if (!brandProfileId) {
      this.loadError.set('يجب اختيار علامة تجارية أولاً.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.brandProfileService.generateOnboardingQuestions(brandProfileId, this.data() ?? {}).subscribe({
      next: res => {
        this.loading.set(false);
        const parsed = this.parseQuestions(res.data?.questionsJson);
        if (parsed.length === 0) {
          this.loadError.set('تعذّر توليد الأسئلة. يمكنك المتابعة مباشرة.');
          this.questions.set([]);
          return;
        }
        this.questions.set(parsed);
        this.currentIndex.set(Math.min(answeredCount, parsed.length));
      },
      error: err => {
        this.loading.set(false);
        this.loadError.set(extractApiErrorMessage(err, 'تعذّر توليد الأسئلة. يمكنك المتابعة مباشرة.'));
        this.questions.set([]);
      },
    });
  }

  private parseQuestions(questionsJson: string | undefined): Question[] {
    if (!questionsJson) return [];
    try {
      const parsed = JSON.parse(questionsJson);
      if (!Array.isArray(parsed)) return [];
      return parsed
        .filter((q): q is Question => typeof q?.question === 'string')
        .map(q => ({ question: q.question, suggestions: Array.isArray(q.suggestions) ? q.suggestions.slice(0, 3) : [] }));
    } catch {
      return [];
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
