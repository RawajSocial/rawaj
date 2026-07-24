import { Component, inject, signal } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { ErrorModal } from './shared/components/error-modal/error-modal';
import { ConfirmDialog } from './shared/components/confirm-dialog/confirm-dialog';
import { PageLoader } from './shared/components/page-loader/page-loader';
import { CelebrationModal } from './shared/components/celebration-modal/celebration-modal';
import { FakePaymentModal } from './shared/components/fake-payment-modal/fake-payment-modal';
import { LoaderService } from './services/loader.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ErrorModal, ConfirmDialog, PageLoader, CelebrationModal, FakePaymentModal],
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
