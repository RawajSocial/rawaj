import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';
import { InviteForm } from './invite-form/invite-form';
import { SeoService } from '../../../services/seo.service';
import { TeamMemberService } from '../../../services/team-member.service';

@Component({
  selector: 'app-invite-login',
  imports: [InviteForm, GsapRevealDirective],
  templateUrl: './invite-login.html',
  styleUrl: './invite-login.css',
})
export class InviteLogin implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly teamMemberService = inject(TeamMemberService);
  private readonly seo = inject(SeoService);

  private readonly token = signal(this.route.snapshot.queryParamMap.get('token') ?? '');

  /** In production the invite token is resolved server-side; this mock
   *  matches it against the local team-member store just to demonstrate
   *  prefilling the invitee's email/name on the frontend. */
  protected readonly invitedMember = computed(() =>
    this.teamMemberService.members().find(m => m.id === this.token() && m.status === 'pending'),
  );

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'دعوة انضمام | رواج',
      description: 'أكمل تسجيل الدخول لقبول دعوتك والانضمام إلى فريق التسويق في رواج.',
      keywords: 'رواج, دعوة انضمام, تسجيل الدخول, فريق التسويق',
      path: '/invite',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
