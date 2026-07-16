import { AfterViewInit, Component, ElementRef } from '@angular/core';
import { GsapRevealDirective } from '../../../shared/directives/gsap-reveal.directive';

@Component({
  selector: 'app-gallery',
  imports: [GsapRevealDirective],
  templateUrl: './gallery.html',
  styleUrl: './gallery.css',
})
export class Gallery implements AfterViewInit {
  constructor(private readonly host: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    const videos = this.host.nativeElement.querySelectorAll<HTMLVideoElement>('video');

    videos.forEach((video) => {
      video.muted = true;
      video.defaultMuted = true;
      video.volume = 0;
    });
  }
}
