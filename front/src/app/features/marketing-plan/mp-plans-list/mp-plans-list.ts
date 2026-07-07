import { Component, input, output } from '@angular/core';
import { SavedPlan } from '../marketing-plan-page/marketing-plan-page';

const SLUG_TO_AR: Record<string, string> = {
  instagram: 'إنستغرام', facebook: 'فيسبوك', tiktok: 'تيك توك',
  snapchat: 'سناب شات', twitter: 'تويتر / X', youtube: 'يوتيوب',
  linkedin: 'لينكدإن',  whatsapp: 'واتساب',
};

const PLATFORM_META: Record<string, { icon: string; color: string }> = {
  'إنستغرام':   { icon: 'fa-brands fa-instagram',  color: '#E1306C' },
  'فيسبوك':     { icon: 'fa-brands fa-facebook-f', color: '#1877F2' },
  'تيك توك':    { icon: 'fa-brands fa-tiktok',     color: '#222'    },
  'سناب شات':   { icon: 'fa-brands fa-snapchat',   color: '#b8960c' },
  'تويتر / X':  { icon: 'fa-brands fa-x-twitter',  color: '#14171A' },
  'يوتيوب':     { icon: 'fa-brands fa-youtube',    color: '#FF0000' },
  'لينكدإن':    { icon: 'fa-brands fa-linkedin',   color: '#0077B5' },
  'واتساب':     { icon: 'fa-brands fa-whatsapp',   color: '#25D366' },
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
