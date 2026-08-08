import { Component, input, output } from '@angular/core';
import { SavedPlan } from '../marketing-plan-page/marketing-plan-page';

const SLUG_TO_AR: Record<string, string> = {
  instagram: 'إنستغرام', facebook: 'فيسبوك',
};

const PLATFORM_META: Record<string, { icon: string; color: string }> = {
  'إنستغرام':   { icon: 'fa-brands fa-instagram',  color: 'var(--color-instagram)' },
  'فيسبوك':     { icon: 'fa-brands fa-facebook-f', color: 'var(--color-facebook)' },
};

@Component({
  selector: 'app-mp-plans-list',
  standalone: true,
  imports: [],
  templateUrl: './mp-plans-list.html',
  styleUrl: './mp-plans-list.css',
})
export class MpPlansList {
  plans = input.required<SavedPlan[]>();

  planSelected = output<string>();
  newPlan      = output<void>();

  platformMeta(slug: string): { icon: string; color: string } {
    const ar = SLUG_TO_AR[slug] ?? slug;
    return PLATFORM_META[ar] ?? PLATFORM_META[slug] ?? { icon: 'fa-solid fa-hashtag', color: '#6b7280' };
  }

  formatDate(ts: number): string {
    return new Date(ts).toLocaleDateString('ar-SA', { year: 'numeric', month: 'long', day: 'numeric' });
  }
}
