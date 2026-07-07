import { Component, HostListener, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CampaignCard } from '../campaign-card/campaign-card';
import { Campaign, CampaignStatus, CampaignPlatform } from '../../../model/campaign.model';

const MOCK_CAMPAIGNS: Campaign[] = [
  {
    id: '1',
    name: 'حملة رمضان الكريم ٢٠٢٥',
    status: 'active',
    platforms: ['instagram', 'facebook', 'tiktok'],
    objective: 'sales',
    industry: 'retail',
    budget: 15000,
    spent: 9300,
    reach: 480000,
    clicks: 12400,
    ctr: 2.58,
    startDate: '٢٠٢٥/٠٣/٠١',
    endDate: '٢٠٢٥/٠٣/٣١',
    createdAt: '2025-02-20',
    adCount: 6,
    coverColor: 'linear-gradient(135deg, #7C3AED, #2563EB)',
  },
  {
    id: '2',
    name: 'إطلاق منتج العيد',
    status: 'paused',
    platforms: ['instagram', 'snapchat'],
    objective: 'awareness',
    budget: 8000,
    spent: 3200,
    reach: 220000,
    clicks: 5600,
    ctr: 2.54,
    startDate: '٢٠٢٥/٠٤/١٠',
    endDate: '٢٠٢٥/٠٤/٣٠',
    createdAt: '2025-04-05',
    adCount: 3,
    coverColor: 'linear-gradient(135deg, #FACC15, #F97316)',
  },
  {
    id: '3',
    name: 'حملة الصيف — التوعية',
    status: 'draft',
    platforms: ['youtube', 'facebook'],
    objective: 'engagement',
    budget: 5000,
    spent: 0,
    reach: 0,
    clicks: 0,
    ctr: 0,
    startDate: '٢٠٢٥/٠٦/٠١',
    endDate: '٢٠٢٥/٠٨/٣١',
    createdAt: '2025-05-15',
    adCount: 0,
    coverColor: 'linear-gradient(135deg, #0EA5E9, #06B6D4)',
  },
];

@Component({
  selector: 'app-campaigns-page',
  standalone: true,
  imports: [RouterLink, CampaignCard],
  templateUrl: './campaigns-page.html',
  styleUrls: ['../../../features/on-boarding/onboarding-shared.css', './campaigns-page.css'],
})
export class CampaignsPage {
  protected readonly campaigns = signal<Campaign[]>(MOCK_CAMPAIGNS);
  protected readonly searchQuery    = signal('');
  protected readonly statusFilter   = signal<CampaignStatus | 'all'>('all');
  protected readonly platformFilter = signal<CampaignPlatform | 'all'>('all');

  protected readonly statusOpen   = signal(false);
  protected readonly platformOpen = signal(false);

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('[data-dd="status"]'))   this.statusOpen.set(false);
    if (!(e.target as HTMLElement).closest('[data-dd="platform"]')) this.platformOpen.set(false);
  }

  protected setStatus(v: CampaignStatus | 'all'): void   { this.statusFilter.set(v);   this.statusOpen.set(false); }
  protected setPlatform(v: CampaignPlatform | 'all'): void { this.platformFilter.set(v); this.platformOpen.set(false); }

  protected readonly statusOptions: { value: CampaignStatus | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع الحالات' },
    { value: 'active',    label: 'نشطة' },
    { value: 'paused',    label: 'موقوفة' },
    { value: 'completed', label: 'مكتملة' },
    { value: 'draft',     label: 'مسودة' },
  ];

  protected readonly platformOptions: { value: CampaignPlatform | 'all'; label: string }[] = [
    { value: 'all',       label: 'جميع المنصات' },
    { value: 'instagram', label: 'إنستغرام' },
    { value: 'facebook',  label: 'فيسبوك' },
    { value: 'tiktok',    label: 'تيك توك' },
    { value: 'youtube',   label: 'يوتيوب' },
    { value: 'snapchat',  label: 'سناب شات' },
    { value: 'linkedin',  label: 'لينكد إن' },
    { value: 'x',         label: 'إكس (تويتر)' },
  ];

  protected readonly filtered = computed(() => {
    const q  = this.searchQuery().toLowerCase().trim();
    const st = this.statusFilter();
    const pl = this.platformFilter();
    return this.campaigns().filter(c => {
      if (q  && !c.name.toLowerCase().includes(q))             return false;
      if (st !== 'all' && c.status !== st)                     return false;
      if (pl !== 'all' && !c.platforms.includes(pl))           return false;
      return true;
    });
  });

  protected get statusLabel():   string { return this.statusOptions.find(o => o.value === this.statusFilter())?.label   ?? ''; }
  protected get platformLabel(): string { return this.platformOptions.find(o => o.value === this.platformFilter())?.label ?? ''; }

  protected pauseCampaign(id: string): void {
    this.campaigns.update(list =>
      list.map(c => c.id === id ? { ...c, status: 'paused' as CampaignStatus } : c)
    );
  }

  protected resumeCampaign(id: string): void {
    this.campaigns.update(list =>
      list.map(c => c.id === id ? { ...c, status: 'active' as CampaignStatus } : c)
    );
  }
}
