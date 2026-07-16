import { Component, OnInit } from '@angular/core';
import { BrandDescription } from './brand-description/brand-description';
import { LoginForm } from './login-form/login-form';
import { SeoService } from '../../../services/seo.service';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-login',
  imports: [BrandDescription, LoginForm,GsapRevealDirective],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'تسجيل الدخول | رواج',
      description: 'سجّل الدخول إلى حسابك في رواج لإدارة حملاتك التسويقية ومحتواك المُولَّد بالذكاء الاصطناعي.',
      keywords: 'رواج, تسجيل الدخول, لوحة التحكم, حملات تسويقية',
      path: '/login',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
