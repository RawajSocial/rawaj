import { Component, OnInit } from '@angular/core';
import { Navbar } from './navbar/navbar';
import { Hero } from './hero/hero';
import { Problems } from './problems/problems';
import { Solutions } from './solutions/solutions';
import { WhyUs } from './why-us/why-us';
import { HowItWorks } from './how-it-works/how-it-works';
import { Gallery } from './gallery/gallery';
import { Posts } from './posts/posts';
import { Pricing } from './pricing/pricing';
import { Cta } from './cta/cta';
import { Footer } from './footer/footer';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-landing',
  imports: [Navbar, Hero, Problems, Solutions, WhyUs, HowItWorks, Gallery, Posts, Pricing, Cta, Footer],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.setPageSeo({
      title: 'Rawaj | AI Marketing Platform for Businesses',
      description:
        'Rawaj is an AI-powered marketing platform to create content, manage campaigns, schedule posts, and track performance from one place.',
      keywords: 'Rawaj, AI marketing, digital marketing, campaign management, content creation, analytics',
      path: '/',
      image: '/home-hero-light.png',
      type: 'website',
    });
  }
}
