import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import { InviteForm } from './invite-form/invite-form';
import { SeoService } from '../../../services/seo.service';
import { TeamMemberService } from '../../../services/team-member.service';
import { AuthService } from '../../../core/auth/auth.service';
import { TenantService } from '../../../core/tenant/tenant.service';
import { InvitationDetailsResponse, ROLE_LABELS } from '../../../model/team-member.model';
import { extractApiErrorMessage } from '../../../core/auth/api-error.util';

@Component({
  selector: 'app-invite-login',
  imports: [InviteForm, GsapRevealDirective, RouterLink],
  templateUrl: './invite-login.html',
  styleUrl: './invite-login.css',
})
export class InviteLogin implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly teamMemberService = inject(TeamMemberService);
  protected readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly seo = inject(SeoService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly details = signal<InvitationDetailsResponse | null>(null);

  protected readonly accepting = signal(false);
  protected readonly acceptError = signal<string | null>(null);
  protected readonly accepted = signal(false);

  protected readonly declining = signal(false);
  protected readonly declineError = signal<string | null>(null);
  protected readonly declined = signal(false);

  /** Whether the currently signed-in user IS the person this invite was addressed to — an
   *  already-authenticated session with a different email must never be able to accept someone
   *  else's invite just by clicking a link. Fails **closed**: if we're authenticated but the
   *  profile hasn't resolved yet (`email` briefly undefined right after page load), this reports a
   *  mismatch rather than `false` — otherwise the accept button could flash for the wrong account
   *  for one tick before the real email loads in.  */
  protected readonly emailMismatch = () => {
    if (!this.authService.isAuthenticated()) return false;
    const invited = this.details()?.email?.toLowerCase();
    if (!invited) return false;
    const email = this.authService.currentUser()?.email?.toLowerCase();
    return !email || email !== invited;
  };

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'دعوة انضمام | رواج',
      description: 'انضم إلى فريق العمل الذي دعاك لإدارة علامته التجارية على رواج.',
      keywords: 'رواج, دعوة انضمام, تسجيل الدخول, فريق العمل',
      path: '/invite',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });

    if (!this.token) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    this.teamMemberService.getInvitationDetails(this.token).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.data) this.details.set(res.data);
        else this.notFound.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      },
    });
  }

  /** Registration path (brand-new invitee) is delegated to `InviteForm`; this only wires up what
   *  happens once the account is created and tokens come back. */
  protected onRegistered(): void {
    this.accepted.set(true);
    this.tenantService.refreshMemberships().subscribe();
    this.tenantService.refresh().subscribe();
    this.authService.fetchMyProfile().subscribe();
    setTimeout(() => this.router.navigateByUrl('/dashboard'), 1200);
  }

  /** Existing-account path: the invitee is already signed in as themselves, so this just calls
   *  the plain accept endpoint (uses the `TenantMember.Id` from `details().tenantMemberId`). */
  protected acceptAsSignedInUser(): void {
    const tenantMemberId = this.details()?.tenantMemberId;
    if (!tenantMemberId) return;
    this.accepting.set(true);
    this.acceptError.set(null);
    this.teamMemberService.acceptInvite(tenantMemberId).subscribe({
      next: res => {
        this.accepting.set(false);
        if (res.status !== 'success') {
          this.acceptError.set(res.message ?? 'تعذّر قبول الدعوة.');
          return;
        }
        this.accepted.set(true);
        this.tenantService.refreshMemberships().subscribe();
        this.tenantService.refresh().subscribe();
        setTimeout(() => this.router.navigateByUrl('/dashboard'), 1200);
      },
      error: err => {
        this.accepting.set(false);
        this.acceptError.set(extractApiErrorMessage(err, 'تعذّر قبول الدعوة.'));
      },
    });
  }

  protected brandInitial(name: string): string {
    return name.trim().charAt(0).toUpperCase() || '؟';
  }

  /** Existing-account path: declines the invite so it stops showing up as pending. */
  protected declineAsSignedInUser(): void {
    const tenantMemberId = this.details()?.tenantMemberId;
    if (!tenantMemberId) return;
    this.declining.set(true);
    this.declineError.set(null);
    this.teamMemberService.declineInvite(tenantMemberId).subscribe({
      next: res => {
        this.declining.set(false);
        if (res.status !== 'success') {
          this.declineError.set(res.message ?? 'تعذّر رفض الدعوة.');
          return;
        }
        this.declined.set(true);
      },
      error: err => {
        this.declining.set(false);
        this.declineError.set(extractApiErrorMessage(err, 'تعذّر رفض الدعوة.'));
      },
    });
  }
}
