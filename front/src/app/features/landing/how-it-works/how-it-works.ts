import { Component, ElementRef, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-how-it-works',
  imports: [RevealDirective],
  templateUrl: './how-it-works.html',
  styleUrl: './how-it-works.css',
})
export class HowItWorks implements AfterViewInit, OnDestroy {
  @ViewChild('timelineContainer', { static: false }) timelineContainer!: ElementRef;
  private observer: IntersectionObserver | null = null;

  ngAfterViewInit() {
    if (typeof window !== 'undefined' && 'IntersectionObserver' in window) {
      this.observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            this.timelineContainer.nativeElement.classList.add('in-view');
            if (this.observer) {
              this.observer.unobserve(entry.target);
            }
          }
        });
      }, {
        threshold: 0.15
      });

      if (this.timelineContainer) {
        this.observer.observe(this.timelineContainer.nativeElement);
      }
    } else {
      if (this.timelineContainer) {
        this.timelineContainer.nativeElement.classList.add('in-view');
      }
    }
  }

  ngOnDestroy() {
    if (this.observer) {
      this.observer.disconnect();
    }
  }
}
