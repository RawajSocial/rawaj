import { Component, OnInit } from '@angular/core';
import { SignUpForm } from './sign-up-form/sign-up-form';
import { SignUpFormDescription } from './sign-up-form-description/sign-up-form-description';
import { SeoService } from '../../../services/seo.service';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-sign-up',
  imports: [SignUpFormDescription, SignUpForm,GsapRevealDirective],
  templateUrl: './sign-up.html',
  styleUrl: './sign-up.css',
})
export class SignUp implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'إنشاء حساب | رواج',
      description:
        'أنشئ حسابك في رواج وابدأ في بناء حملاتك التسويقية وتوليد المحتوى بالذكاء الاصطناعي مجانًا.',
      keywords: 'رواج, إنشاء حساب, تسجيل, منصة تسويق بالذكاء الاصطناعي',
      path: '/sign-up',
      image: '/home-hero-light.png',
      type: 'website',
      noIndex: true,
    });
  }
}
