import { Injectable, signal } from '@angular/core';
import { GeneratedItem } from '../model/generated-item.model';

const INITIAL_ITEMS: GeneratedItem[] = [
  {
    id: '1', type: 'static-ad', title: 'منتج العناية بالبشرة', brand: 'براند كير',
    status: 'generated', createdAt: '2026-06-05T10:40:00',
    size: 'square', tone: 'professional', language: 'ar',
    description: 'إعلان لمنتج العناية بالبشرة',
    thumbnailUrl: '/Ads/Product%201.jpg',
    brandProfileId: 'bp1', campaignId: '1',
  },
  {
    id: '2', type: 'static-ad', title: 'كوليكشن الصيف', brand: 'ستايل هاوس',
    status: 'generated', createdAt: '2026-06-04T09:00:00',
    size: 'portrait', tone: 'energetic', language: 'ar',
    description: 'مجموعة الصيف الجديدة بأسعار مميزة',
    thumbnailUrl: '/Ads/product%202.jpg',
    brandProfileId: 'bp2', campaignId: '2',
  },
  {
    id: '3', type: 'static-ad', title: 'تشكيلة جديدة', brand: 'تاون تيم',
    status: 'generated', createdAt: '2026-06-03T14:20:00',
    size: 'landscape', tone: 'casual', language: 'ar',
    description: 'تشكيلة الشباب الجديدة',
    thumbnailUrl: '/Ads/product-3.jpg',
  },
  {
    id: '4', type: 'static-ad', title: 'عطر لارو الفاخر', brand: 'لارو للعطور',
    status: 'generated', createdAt: '2026-06-03T11:00:00',
    size: 'square', tone: 'luxurious', language: 'ar',
    description: 'عطر فاخر للمناسبات الراقية',
    thumbnailUrl: '/Ads/perfume.jpeg',
  },
  {
    id: '5', type: 'static-ad', title: 'أحذية السيزون', brand: 'ستيب ستايل',
    status: 'generated', createdAt: '2026-06-02T16:30:00',
    size: 'square', tone: 'energetic', language: 'ar',
    description: 'أحدث تصاميم الأحذية لهذا السيزون',
    thumbnailUrl: '/Ads/shoes.webp',
  },
  {
    id: '6', type: 'video', title: 'فيديو إعلاني 1', brand: 'براند X',
    status: 'generated', createdAt: '2026-06-02T10:00:00',
    tone: 'professional', language: 'ar',
    description: 'فيديو ترويجي للمنتجات الجديدة',
    videoUrl: '/Ads/ad-video.mp4',
  },
  {
    id: '7', type: 'video', title: 'فيديو إعلاني 2', brand: 'تاون تيم',
    status: 'generated', createdAt: '2026-06-01T08:00:00',
    tone: 'energetic', language: 'ar',
    description: 'إعلان حملة الصيف',
    videoUrl: '/Ads/ad-video2.mp4',
  },
  {
    id: '8', type: 'video', title: 'إعلان الساعات الفاخرة', brand: 'واتش ستور',
    status: 'generated', createdAt: '2026-05-30T12:00:00',
    tone: 'luxurious', language: 'ar',
    description: 'مجموعة الساعات الفاخرة الجديدة',
    videoUrl: '/Ads/watch.mp4',
  },
  {
    id: '9', type: 'text', title: 'منشور إنستغرام', brand: 'براند X',
    status: 'generated', createdAt: '2026-06-01T13:00:00',
    tone: 'professional', language: 'ar',
    description: 'منشور للإطلاق الجديد',
    textContent: '🌟 أطلقنا مجموعتنا الجديدة!\n\nاكتشف أحدث صيحات الموضة مع خصومات حصرية تصل إلى 40%.\nتسوق الآن واستمتع بتجربة فريدة لا تُنسى.',
  },
  {
    id: '10', type: 'text', title: 'هاشتاقات تسويقية', brand: 'ستايل هاوس',
    status: 'generated', createdAt: '2026-05-31T10:00:00',
    tone: 'energetic', language: 'ar',
    description: 'هاشتاقات لحملة الصيف',
    textContent: '#صيف_2026 #موضة #عروض_حصرية #تسوق_اونلاين #خصومات #ستايل #ملابس_عصرية #ستايل_هاوس',
  },
];

let _nextId = 200;

@Injectable({ providedIn: 'root' })
export class MediaService {
  readonly items = signal<GeneratedItem[]>(INITIAL_ITEMS);

  nextId(): string {
    return String(++_nextId);
  }

  add(item: GeneratedItem): void {
    this.items.update(list => [item, ...list]);
  }

  markGenerated(id: string, extra?: Partial<GeneratedItem>): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, status: 'generated', ...extra } : i)
    );
  }

  /** Puts an already-generated item back into the "generating" state — used
   *  when re-running generation with an edit prompt on top of an existing
   *  result. */
  markRegenerating(id: string): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, status: 'generating' } : i)
    );
  }

  markFailed(id: string): void {
    this.items.update(list =>
      list.map(i => i.id === id ? { ...i, status: 'failed' } : i)
    );
  }

  /** Swaps a locally-assigned placeholder id for the real id the backend returned once
   *  generation succeeds — callers must re-point any signal holding the old id (e.g. the page's
   *  "currently selected" item) themselves, since this only touches the stored list. */
  replaceId(oldId: string, newId: string): void {
    if (oldId === newId) return;
    this.items.update(list => list.map(i => i.id === oldId ? { ...i, id: newId } : i));
  }

  update(updated: GeneratedItem): void {
    this.items.update(list => list.map(i => i.id === updated.id ? updated : i));
  }

  remove(id: string): void {
    this.items.update(list => list.filter(i => i.id !== id));
  }
}
