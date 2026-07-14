import { Component, signal } from '@angular/core';

const SCROLLED_THRESHOLD = 40;
const HIDE_THRESHOLD = 120;

@Component({
  selector: 'app-navbar',
  imports: [],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  host: {
    '(window:scroll)': 'onScroll()',
    '[class.navbar--scrolled]': 'scrolled()',
    '[class.navbar--hidden]': 'hidden()',
  },
})
export class Navbar {
  protected readonly scrolled = signal(false);
  protected readonly hidden = signal(false);

  private lastScrollY = 0;

  onScroll(): void {
    const currentScrollY = window.scrollY;

    this.scrolled.set(currentScrollY > SCROLLED_THRESHOLD);
    this.hidden.set(
      currentScrollY > HIDE_THRESHOLD && currentScrollY > this.lastScrollY
    );

    this.lastScrollY = currentScrollY;
  }
}
