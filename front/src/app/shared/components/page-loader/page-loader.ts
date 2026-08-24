import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LoaderService } from '../../../services/loader.service';

@Component({
  selector: 'app-page-loader',
  imports: [],
  templateUrl: './page-loader.html',
  styleUrl: './page-loader.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageLoader {
  private readonly loaderService = inject(LoaderService);
  protected readonly loading = this.loaderService.loading;
}
