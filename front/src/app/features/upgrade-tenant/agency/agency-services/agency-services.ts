import { Component, computed, effect, input, output, signal } from '@angular/core';
import { UpgradeTenantData } from '../../upgrade-tenant-page/upgrade-tenant-page';

const SERVICES = [
  { label: 'إدارة السوشيال ميديا',   icon: 'fa-solid fa-hashtag' },
  { label: 'إنتاج المحتوى',           icon: 'fa-solid fa-pen-nib' },
  { label: 'الإعلانات المدفوعة',      icon: 'fa-solid fa-bullhorn' },
  { label: 'تحسين محركات البحث',      icon: 'fa-solid fa-magnifying-glass' },
  { label: 'التسويق بالبريد',          icon: 'fa-solid fa-envelope' },
  { label: 'الهوية البصرية',           icon: 'fa-solid fa-palette' },
  { label: 'إنتاج الفيديو',            icon: 'fa-solid fa-film' },
  { label: 'التسويق بالمؤثرين',        icon: 'fa-solid fa-star' },
  { label: 'تصميم المواقع',            icon: 'fa-solid fa-laptop-code' },
  { label: 'التصوير الفوتوغرافي',      icon: 'fa-solid fa-camera' },
];

@Component({
  selector: 'app-agency-services',
  imports: [],
  templateUrl: './agency-services.html',
  styleUrls: ['../../upgrade-tenant-shared.css', './agency-services.css'],
})
export class AgencyServices {
  readonly data = input<UpgradeTenantData>({});
  readonly next = output<void>();
  readonly back = output<void>();
  readonly dataChange = output<Partial<UpgradeTenantData>>();

  protected readonly services  = SERVICES;
  protected readonly selected  = signal<string[]>([]);
  protected readonly showError = signal(false);
  protected readonly isValid   = computed(() => this.selected().length > 0);

  constructor() {
    effect(() => {
      const s = this.data().primaryServices;
      if (s?.length) this.selected.set([...s]);
    });
  }

  protected toggle(label: string): void {
    this.showError.set(false);
    this.selected.update(list =>
      list.includes(label) ? list.filter(l => l !== label) : [...list, label]
    );
  }

  protected isSelected(label: string): boolean {
    return this.selected().includes(label);
  }

  protected onNext(): void {
    if (!this.isValid()) { this.showError.set(true); return; }
    this.dataChange.emit({ primaryServices: this.selected() });
    this.next.emit();
  }
}
