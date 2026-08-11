import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Footer } from '../../landing/footer/footer';

/** Shared chrome for the standalone legal pages (Privacy Policy, Terms & Conditions) — a topbar
 *  matching the landing page navbar's logo styling, plus a title/effective-date header and the
 *  same site footer. Page content is passed in via content projection since it's static,
 *  translated legal text with no interactivity. */
@Component({
  selector: 'app-legal-page-layout',
  imports: [RouterLink, Footer],
  templateUrl: './legal-page-layout.html',
  styleUrl: './legal-page-layout.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalPageLayout {
  readonly title = input.required<string>();
  readonly effectiveDate = input.required<string>();
}
