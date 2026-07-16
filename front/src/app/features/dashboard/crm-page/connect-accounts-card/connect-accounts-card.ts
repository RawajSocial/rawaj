import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ConnectPlatform } from '../crm-page.model';

const CONNECT_PLATFORMS: ConnectPlatform[] = [
  { key: 'instagram', label: 'إنستغرام',  icon: 'fa-brands fa-instagram',   color: 'var(--color-instagram)' },
  { key: 'facebook',  label: 'فيسبوك',    icon: 'fa-brands fa-facebook-f',  color: 'var(--color-facebook)' },
  { key: 'tiktok',    label: 'تيك توك',   icon: 'fa-brands fa-tiktok',      color: 'var(--gradient-tiktok)' },
  { key: 'snapchat',  label: 'سناب شات',  icon: 'fa-brands fa-snapchat',    color: 'var(--color-snapchat)' },
  { key: 'linkedin',  label: 'لينكدإن',   icon: 'fa-brands fa-linkedin-in', color: 'var(--color-linkedin)' },
  { key: 'youtube',   label: 'يوتيوب',    icon: 'fa-brands fa-youtube',     color: 'var(--color-youtube)' },
];

@Component({
  selector: 'app-connect-accounts-card',
  imports: [],
  templateUrl: './connect-accounts-card.html',
  styleUrls: ['../../dashboard-shared.css', './connect-accounts-card.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectAccountsCard {
  protected readonly connectPlatforms = CONNECT_PLATFORMS;

  // TODO: back this with the real OAuth connect/disconnect flow once the
  // backend endpoints exist — for now this only flips local UI state.
  private readonly connectedAccounts = signal(new Set(['instagram', 'facebook']));

  protected isConnected(key: string): boolean {
    return this.connectedAccounts().has(key);
  }

  protected toggleConnect(key: string): void {
    this.connectedAccounts.update(set => {
      const next = new Set(set);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }
}
