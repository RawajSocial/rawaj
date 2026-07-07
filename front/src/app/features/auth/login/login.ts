import { Component, OnInit } from '@angular/core';
import { BrandDescription } from './brand-description/brand-description';
import { LoginForm } from './login-form/login-form';
import { SeoService } from '../../../services/seo.service';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-login',
  imports: [BrandDescription, LoginForm,RevealDirective],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'تسجيل الدخول | Rawaj',
      description: 'Sign in to your Rawaj account to manage your AI-powered marketing campaigns and content.',
      keywords: 'Rawaj login, sign in, marketing dashboard, AI campaigns',
      path: '/login',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
