import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

export type SeoConfig = {
  title: string;
  description: string;
  keywords?: string;
  path?: string;
  image?: string;
  type?: 'website' | 'article';
  noIndex?: boolean;
};

@Injectable({ providedIn: 'root' })
export class SeoService {
  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  setPageSeo(config: SeoConfig): void {
    const pagePath = this.normalizePath(config.path ?? '/');
    const pageUrl = this.getAbsoluteUrl(pagePath);
    const imageUrl = this.getAbsoluteUrl(config.image ?? '/home-hero-light.png');
    const robotsContent = config.noIndex ? 'noindex, nofollow' : 'index, follow';

    this.title.setTitle(config.title);

    this.meta.updateTag({ name: 'description', content: config.description });
    this.meta.updateTag({ name: 'keywords', content: config.keywords ?? '' });
    this.meta.updateTag({ name: 'robots', content: robotsContent });

    this.meta.updateTag({ property: 'og:title', content: config.title });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:type', content: config.type ?? 'website' });
    this.meta.updateTag({ property: 'og:url', content: pageUrl });
    this.meta.updateTag({ property: 'og:image', content: imageUrl });
    this.meta.updateTag({ property: 'og:locale', content: 'ar_AR' });
    this.meta.updateTag({ property: 'og:site_name', content: 'Rawaj' });

    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: config.title });
    this.meta.updateTag({ name: 'twitter:description', content: config.description });
    this.meta.updateTag({ name: 'twitter:image', content: imageUrl });

    this.updateCanonical(pageUrl);
  }

  private normalizePath(path: string): string {
    if (!path || path === '/') {
      return '/';
    }
    return path.startsWith('/') ? path : `/${path}`;
  }

  private getAbsoluteUrl(path: string): string {
    return new URL(path, this.document.location.origin).toString();
  }

  private updateCanonical(url: string): void {
    let link = this.document.querySelector("link[rel='canonical']") as HTMLLinkElement | null;
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }
}
