import { Component, inject, signal } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { ErrorModal } from './shared/components/error-modal/error-modal';
import { PageLoader } from './shared/components/page-loader/page-loader';
import { LoaderService } from './services/loader.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ErrorModal, PageLoader],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('rawaj-front');

  private readonly loaderService = inject(LoaderService);

  constructor() {
    let navigationPending = false;

    inject(Router).events.subscribe(event => {
      if (event instanceof NavigationStart) {
        navigationPending = true;
        this.loaderService.show();
      } else if (
        (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) &&
        navigationPending
      ) {
        navigationPending = false;
        this.loaderService.hide();
      }
    });
  }
}
