import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import {
  AiTrialApiService,
  GenerateTrialContentResponse,
  GenerateTrialImageResponse,
} from '../../../core/api/ai-trial-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ApiError } from '../../../core/api';
import { TeamMembersApiService } from '../../../core/api/team-members-api.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { PendingInviteSummary } from '../../../core/models';

type TrialMode = 'content' | 'image';

@Component({
  selector: 'app-welcome',
  imports: [FormsModule, RouterLink],
  templateUrl: './welcome.html',
  styleUrls: ['../../dashboard/users/users-shared.css', './welcome.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Welcome {
  private readonly aiTrialApi = inject(AiTrialApiService);
  private readonly authService = inject(AuthService);
  private readonly teamMembersApi = inject(TeamMembersApiService);
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);

  protected readonly trialMode = signal<TrialMode>('content');
  protected readonly prompt = signal('');
  protected readonly generating = signal(false);
  protected readonly generateError = signal<string | null>(null);
  protected readonly resultText = signal<string | null>(null);
  protected readonly resultImage = signal<string | null>(null);
  protected readonly remainingTrials = signal<number | null>(null);

  protected readonly pendingInvites = signal<PendingInviteSummary[]>([]);
  protected readonly invitesLoading = signal(false);
  protected readonly respondingInviteId = signal<string | null>(null);
  protected readonly inviteError = signal<string | null>(null);

  constructor() {
    this.loadPendingInvites();
  }

  private loadPendingInvites(): void {
    this.invitesLoading.set(true);
    this.teamMembersApi.getMyPendingInvites().subscribe({
      next: (invites) => {
        this.pendingInvites.set(invites);
        this.invitesLoading.set(false);
      },
      error: () => this.invitesLoading.set(false),
    });
  }

  protected acceptInvite(tenantMemberId: string): void {
    this.respondingInviteId.set(tenantMemberId);
    this.inviteError.set(null);

    this.teamMembersApi.acceptInvite(tenantMemberId).subscribe({
      next: () => {
        this.tenantService.loadContext().subscribe({
          next: () => void this.router.navigate(['/dashboard']),
          error: () => void this.router.navigate(['/dashboard']),
        });
      },
      error: (error: unknown) => {
        this.respondingInviteId.set(null);
        this.inviteError.set(error instanceof ApiError ? error.message : 'تعذر قبول الدعوة، حاول مرة أخرى.');
      },
    });
  }

  protected declineInvite(tenantMemberId: string): void {
    this.respondingInviteId.set(tenantMemberId);
    this.inviteError.set(null);

    this.teamMembersApi.declineInvite(tenantMemberId).subscribe({
      next: () => {
        this.respondingInviteId.set(null);
        this.pendingInvites.update((invites) => invites.filter((i) => i.tenantMemberId !== tenantMemberId));
      },
      error: (error: unknown) => {
        this.respondingInviteId.set(null);
        this.inviteError.set(error instanceof ApiError ? error.message : 'تعذر رفض الدعوة، حاول مرة أخرى.');
      },
    });
  }

  protected setTrialMode(mode: TrialMode): void {
    this.trialMode.set(mode);
    this.resultText.set(null);
    this.resultImage.set(null);
    this.generateError.set(null);
  }

  protected generate(): void {
    const prompt = this.prompt().trim();
    if (!prompt || this.generating()) return;

    this.generating.set(true);
    this.generateError.set(null);
    this.resultText.set(null);
    this.resultImage.set(null);

    const request$: Observable<GenerateTrialContentResponse | GenerateTrialImageResponse> =
      this.trialMode() === 'content'
        ? this.aiTrialApi.generateContent(prompt)
        : this.aiTrialApi.generateImage(prompt);

    request$.subscribe({
      next: (result) => {
        this.generating.set(false);
        this.remainingTrials.set(result.remainingTrialsToday);
        if ('generatedText' in result) {
          this.resultText.set(result.generatedText);
        } else {
          this.resultImage.set(result.imageDataUrl);
        }
      },
      error: (error: unknown) => {
        this.generating.set(false);
        this.generateError.set(
          error instanceof ApiError ? error.message : 'تعذر توليد المحتوى، حاول مرة أخرى.',
        );
      },
    });
  }

  protected logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
