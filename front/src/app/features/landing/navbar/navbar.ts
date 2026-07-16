import { Component, OnDestroy, afterNextRender, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

const SCROLLED_THRESHOLD = 40;
const HIDE_THRESHOLD = 120;

interface NavItem {
  id: string;
  label: string;
}

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  host: {
    '(window:scroll)': 'onScroll()',
    '[class.navbar--scrolled]': 'scrolled()',
    '[class.navbar--hidden]': 'hidden()',
  },
})
export class Navbar implements OnDestroy {
  protected readonly scrolled = signal(false);
  protected readonly hidden = signal(false);

  protected readonly navItems: NavItem[] = [
    { id: 'hero', label: 'الرئيسية' },
    { id: 'problems', label: 'المشكلة' },
    { id: 'solutions', label: 'الحلول' },
    { id: 'why-us', label: 'لماذا رواج' },
    { id: 'how-it-works', label: 'كيف يعمل' },
    { id: 'pricing', label: 'الأسعار' },
  ];

  protected readonly activeSection = signal('hero');

  private lastScrollY = 0;
  private sectionObserver: IntersectionObserver | null = null;

  constructor() {
    afterNextRender(() => this.observeSections());
  }

  ngOnDestroy(): void {
    this.sectionObserver?.disconnect();
  }

  onScroll(): void {
    const currentScrollY = window.scrollY;

    this.scrolled.set(currentScrollY > SCROLLED_THRESHOLD);
    this.hidden.set(
      currentScrollY > HIDE_THRESHOLD && currentScrollY > this.lastScrollY
    );

    this.lastScrollY = currentScrollY;
  }

  protected closeMobileMenu(): void {
    document.getElementById('navMenu')?.classList.remove('show');
  }

  private observeSections(): void {
    this.sectionObserver = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            this.activeSection.set(entry.target.id);
          }
        }
      },
      { rootMargin: '-45% 0px -50% 0px', threshold: 0 }
    );

    for (const item of this.navItems) {
      const el = document.getElementById(item.id);
      if (el) this.sectionObserver.observe(el);
    }
  }
}
