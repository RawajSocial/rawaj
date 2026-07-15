import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="not-found">
      <h1>404</h1>
      <p>الصفحة غير موجودة.</p>
      <a routerLink="/" class="btn btn-primary">العودة للرئيسية</a>
    </div>
  `,
  styles: `
    .not-found {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      text-align: center;
    }

    h1 {
      font-size: 4rem;
      font-weight: 800;
      margin: 0;
    }
  `,
})
export class NotFound {}
