import { Component, OnInit } from '@angular/core';
import { SignUpForm } from './sign-up-form/sign-up-form';
import { SignUpFormDescription } from './sign-up-form-description/sign-up-form-description';
import { SeoService } from '../../../services/seo.service';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-sign-up',
  imports: [SignUpFormDescription, SignUpForm,RevealDirective],
  templateUrl: './sign-up.html',
  styleUrl: './sign-up.css',
})
export class SignUp implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'انشاء حساب | Rawaj',
      description:
        'Create your Rawaj account and start building AI-powered marketing content, campaigns, and performance workflows.',
      keywords: 'Rawaj sign up, create account, AI marketing platform, register',
      path: '/sign-up',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
