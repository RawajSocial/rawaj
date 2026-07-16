import { Component, input, output, signal } from '@angular/core';
import { OnboardingStepHeader } from '../onboarding-step-header/onboarding-step-header';
import { OnboardingStepActions } from '../onboarding-step-actions/onboarding-step-actions';
import { StepBadge } from '../../../shared/components/step-badge/step-badge';
import { StepHeading } from '../../../shared/components/step-heading/step-heading';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

const MAX_IMAGES = 10;

@Component({
  selector: 'app-onboarding-step-six',
  imports: [GsapRevealDirective, OnboardingStepHeader, OnboardingStepActions, StepBadge, StepHeading],
  templateUrl: './onboarding-step-six.html',
  styleUrl: './onboarding-step-six.css',
})
export class OnboardingStepSix {
  readonly currentStep = input(6);
  readonly totalSteps = input(7);
  readonly data = input<OnboardingData | null>(null);
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<OnboardingData>>();

  protected readonly maxImages = MAX_IMAGES;
  protected readonly previews = signal<ImagePreview[]>([]);
  protected isDragging = false;

  protected handleFiles(fileList: FileList | null): void {
    if (!fileList) return;
    const current = [...this.previews()];
    const remaining = MAX_IMAGES - current.length;
    const files = Array.from(fileList).slice(0, remaining);

    files.forEach(file => {
      if (!file.type.startsWith('image/')) return;
      const reader = new FileReader();
      reader.onload = (e) => {
        const updated = [...this.previews(), { name: file.name, url: e.target?.result as string }];
        this.previews.set(updated);
        this.emit({ campaignPhotos: updated.map(p => p.name) });
      };
      reader.readAsDataURL(file);
    });
  }

  protected removeImage(index: number): void {
    const updated = this.previews().filter((_, i) => i !== index);
    this.previews.set(updated);
    this.emit({ campaignPhotos: updated.map(p => p.name) });
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  protected onDragLeave(): void { this.isDragging = false; }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    this.handleFiles(event.dataTransfer?.files ?? null);
  }

  protected emit(partial: Partial<OnboardingData>): void { this.dataChange.emit(partial); }
  protected onNext(): void { this.next.emit(); }
  protected onBack(): void { this.back.emit(); }
}

type ImagePreview = { name: string; url: string };

type OnboardingData = {
  campaignPhotos?: string[];
  hashtags?: string;
  additionalNotes?: string;
};
