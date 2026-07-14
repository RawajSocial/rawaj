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
import { TeamMember, TeamProject } from '../../../../model/team-member.model';
import { animateModalIn, animateModalOut } from '../../../../shared/utils/modal-motion';

@Component({
  selector: 'app-assign-projects-modal',
  imports: [],
  templateUrl: './assign-projects-modal.html',
  styleUrls: ['../users-shared.css', './assign-projects-modal.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignProjectsModal {
  readonly open = input.required<boolean>();
  readonly member = input<TeamMember | null>(null);
  readonly projects = input.required<TeamProject[]>();

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
      this.selectedIds.set(new Set(this.member()?.assignedProjectIds ?? []));
      queueMicrotask(() => {
        const panel = this.panelRef()?.nativeElement;
        const backdrop = this.backdropRef()?.nativeElement;
        if (panel && backdrop) animateModalIn(panel, backdrop);
      });
    });
  }

  protected toggle(projectId: string): void {
    this.selectedIds.update(current => {
      const next = new Set(current);
      if (next.has(projectId)) next.delete(projectId);
      else next.add(projectId);
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
