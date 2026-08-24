import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { BrandProfile } from '../../../../model/brand-profile.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

/** Assigns which brand profiles a team member can work on — repurposed from the old mock
 *  "projects" concept now that مشاريعي/campaigns are real brand profiles, not invented projects. */
@Component({
  selector: 'app-assign-projects-modal',
  imports: [],
  templateUrl: './assign-projects-modal.html',
  styleUrls: ['../users-shared.css', './assign-projects-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignProjectsModal {
  readonly open = input.required<boolean>();
  readonly memberName = input<string>('');
  readonly assignedBrandProfileIds = input<string[]>([]);
  readonly brandProfiles = input.required<BrandProfile[]>();

  readonly closed = output<void>();
  readonly assigned = output<string[]>();

  protected readonly selectedIds = signal<Set<string>>(new Set());

  private readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly backdropRef = viewChild<ElementRef<HTMLElement>>('backdrop');
  private readonly closing = signal(false);

  constructor() {
    effect(() => {
      if (!this.open()) return;
      this.closing.set(false);
      this.selectedIds.set(new Set(this.assignedBrandProfileIds()));
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
    });
  }

  protected toggle(brandProfileId: string): void {
    this.selectedIds.update(current => {
      const next = new Set(current);
      if (next.has(brandProfileId)) next.delete(brandProfileId);
      else next.add(brandProfileId);
      return next;
    });
  }

  protected requestClose(): void {
    if (this.closing()) return;
    const panel = this.panelRef()?.nativeElement;
    const backdrop = this.backdropRef()?.nativeElement;
    if (!panel || !backdrop) {
      this.closed.emit();
      return;
    }
    this.closing.set(true);
    animateModalOut(panel, backdrop).then(() => this.closed.emit());
  }

  protected submit(): void {
    this.assigned.emit(Array.from(this.selectedIds()));
  }
}
