import { Component, HostListener, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MediaService } from '../../../services/media.service';
import { GeneratedItem, GenType, TYPE_CFG } from '../../../model/generated-item.model';
import { MediaToolbar } from '../media-toolbar/media-toolbar';
import { MediaCard } from '../media-card/media-card';
import { MediaViewer } from '../media-viewer/media-viewer';

@Component({
  selector: 'app-my-media-page',
  standalone: true,
  imports: [RouterLink, MediaToolbar, MediaCard, MediaViewer],
  templateUrl: './my-media-page.html',
  styleUrls: [
    '../../../features/on-boarding/onboarding-shared.css',
    './my-media-page.css',
  ],
})
export class MyMediaPage {
  readonly typeCfg = TYPE_CFG;

  filterType  = signal<GenType | 'all'>('all');
  searchQuery = signal('');
  selectedItem = signal<GeneratedItem | null>(null);

  readonly filteredItems = computed(() => {
    const ft = this.filterType();
    const q  = this.searchQuery().trim().toLowerCase();
    return this.media.items().filter(i => {
      const typeMatch  = ft === 'all' || i.type === ft;
      const queryMatch = !q || i.title.toLowerCase().includes(q) || i.brand.toLowerCase().includes(q);
      return typeMatch && queryMatch;
    });
  });

  readonly counts = computed(() => ({
    all:          this.media.items().length,
    'static-ad':  this.media.items().filter(i => i.type === 'static-ad').length,
    video:        this.media.items().filter(i => i.type === 'video').length,
    text:         this.media.items().filter(i => i.type === 'text').length,
  }));

  constructor(private media: MediaService) {}

  @HostListener('document:keydown.escape')
  onEsc(): void {
    // viewer handles its own ESC; this closes if viewer is not open
    if (!this.selectedItem()) return;
  }

  openItem(item: GeneratedItem): void { this.selectedItem.set(item); }
  closeModal(): void                  { this.selectedItem.set(null); }
}
