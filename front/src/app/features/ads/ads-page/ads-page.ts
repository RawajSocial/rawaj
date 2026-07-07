import { Component, HostListener, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdCard } from '../ad-card/ad-card';
import { Ad, AdStatus, AdFormat } from '../../../model/ad.model';
import { CampaignPlatform } from '../../../model/campaign.model';

const MOCK_ADS: Ad[] = [
  { id: 'a1', name: 'إعلان الشريحة الرئيسية — رمضان',   campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platform: 'instagram', status: 'active',    format: 'carousel', impressions: 184000, clicks: 4700,  ctr: 2.55, spend: 3200, cpc: 0.68, createdAt: '2025-03-01', thumbnailColor: 'linear-gradient(135deg,#7C3AED,#2563EB)' },
  { id: 'a2', name: 'فيديو قصير — منتج العيد',            campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platform: 'tiktok',    status: 'active',    format: 'reel',     impressions: 210000, clicks: 5800,  ctr: 2.76, spend: 2800, cpc: 0.48, createdAt: '2025-03-05', thumbnailColor: 'linear-gradient(135deg,#010101,#3B3B3B)' },
  { id: 'a3', name: 'ستوري الخصم ٣٠٪',                   campaignId: '1', campaignName: 'حملة رمضان الكريم ٢٠٢٥',   platform: 'instagram', status: 'paused',    format: 'story',    impressions: 86000,  clicks: 1900,  ctr: 2.21, spend: 1300, cpc: 0.68, createdAt: '2025-03-10', thumbnailColor: 'linear-gradient(135deg,#E1306C,#833AB4)' },
  { id: 'a4', name: 'إعلان منتج العيد — فيسبوك',          campaignId: '2', campaignName: 'إطلاق منتج العيد',          platform: 'facebook',  status: 'paused',    format: 'image',    impressions: 120000, clicks: 3100,  ctr: 2.58, spend: 1800, cpc: 0.58, createdAt: '2025-04-10', thumbnailColor: 'linear-gradient(135deg,#1877F2,#0D4FA0)' },
  { id: 'a5', name: 'فيديو تعريفي — سناب شات',            campaignId: '2', campaignName: 'إطلاق منتج العيد',          platform: 'snapchat',  status: 'pending',   format: 'video',    impressions: 100000, clicks: 2500,  ctr: 2.50, spend: 1400, cpc: 0.56, createdAt: '2025-04-12', thumbnailColor: 'linear-gradient(135deg,#FFFC00,#F0B800)' },
  { id: 'a6', name: 'إعلان الوعي — يوتيوب',               campaignId: '3', campaignName: 'حملة الصيف — التوعية',      platform: 'youtube',   status: 'pending',   format: 'video',    impressions: 0,      clicks: 0,     ctr: 0,    spend: 0,    cpc: 0,    createdAt: '2025-05-15', thumbnailColor: 'linear-gradient(135deg,#FF0000,#CC0000)' },
];

@Component({
  selector: 'app-ads-page',
  standalone: true,
  imports: [RouterLink, AdCard],
  templateUrl: './ads-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './ads-page.css'],
})
export class AdsPage {
  protected readonly ads            = signal<Ad[]>(MOCK_ADS);
  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<AdStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');
  protected readonly formatFilter   = signal<AdFormat | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);
  protected readonly formatOpen   = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!t.closest('[data-dd="platform"]')) this.platformOpen.set(false);
    if (!t.closest('[data-dd="format"]'))   this.formatOpen.set(false);
  }

  protected setStatus(v: AdStatus | 'all'): void              { this.statusFilter.set(v);   this.statusOpen.set(false); }
  protected setPlatform(v: CampaignPlatform | 'all'): void    { this.platformFilter.set(v); this.platformOpen.set(false); }
  protected setFormat(v: AdFormat | 'all'): void              { this.formatFilter.set(v);   this.formatOpen.set(false); }

  protected get statusLabel():   string { return this.statusOptions.find(o   => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }
  protected get formatLabel():   string { return this.formatOptions.find(o   => o.value === this.formatFilter())?.label   ?? ''; }

  protected readonly statusOptions: { value: AdStatus | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع الحالات' }, { value: 'active', label: 'نشط' },
    { value: 'paused', label: 'موقوف' }, { value: 'pending', label: 'قيد المراجعة' },
    { value: 'rejected', label: 'مرفوض' }, { value: 'completed', label: 'مكتمل' },
  ];

  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع المنصات' }, { value: 'instagram', label: 'إنستغرام' },
    { value: 'facebook', label: 'فيسبوك' }, { value: 'tiktok', label: 'تيك توك' },
    { value: 'youtube', label: 'يوتيوب' }, { value: 'snapchat', label: 'سناب شات' },
    { value: 'linkedin', label: 'لينكد إن' }, { value: 'x', label: 'إكس (تويتر)' },
  ];

  protected readonly formatOptions: { value: AdFormat | 'all'; label: string }[] = [
    { value: 'all', label: 'جميع الأشكال' }, { value: 'image', label: 'صورة' },
    { value: 'video', label: 'فيديو' }, { value: 'carousel', label: 'كاروسيل' },
    { value: 'story', label: 'ستوري' }, { value: 'reel', label: 'ريلز' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    const fm = this.formatFilter();
    return this.ads().filter(a => {
      if (q  && !a.name.toLowerCase().includes(q) && !a.campaignName.toLowerCase().includes(q)) return false;
      if (st !== 'all' && a.status   !== st) return false;
      if (pl !== 'all' && a.platform !== pl) return false;
      if (fm !== 'all' && a.format   !== fm) return false;
      return true;
    });
  });

  protected toggleAd(id: string): void {
    this.ads.update(list =>
      list.map(a => a.id === id
        ? { ...a, status: (a.status === 'active' ? 'paused' : 'active') as AdStatus }
        : a
      )
    );
  }
}
