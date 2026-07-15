import { AfterViewInit, Component, ElementRef } from '@angular/core';
import { RevealDirective } from '../../../shared/directives/reveal.directive';

@Component({
  selector: 'app-gallery',
  imports: [RevealDirective],
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
