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

  /** Whether the currently signed-in user IS the person this invite was addressed to — an
   *  already-authenticated session with a different email must never be able to accept someone
   *  else's invite just by clicking a link. */
  protected readonly emailMismatch = () => {
    const email = this.authService.currentUser()?.email?.toLowerCase();
    const invited = this.details()?.email?.toLowerCase();
    return !!email && !!invited && email !== invited;
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
        setTimeout(() => this.router.navigateByUrl('/dashboard'), 1200);
      },
      error: err => {
        this.accepting.set(false);
        this.acceptError.set(extractApiErrorMessage(err, 'تعذّر قبول الدعوة.'));
      },
    });
  }
}
