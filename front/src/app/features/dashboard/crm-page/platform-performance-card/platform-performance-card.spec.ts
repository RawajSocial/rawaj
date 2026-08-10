import { TestBed } from '@angular/core/testing';
import { PlatformPerformanceCard } from './platform-performance-card';
import { PlatformStat } from '../crm-page.model';

function stat(overrides: Partial<PlatformStat>): PlatformStat {
  return {
    key: 'instagram',
    label: 'إنستغرام',
    icon: 'fa-brands fa-instagram',
    color: '#000',
    followers: 0,
    uniqueViewers: 0,
    engagementRate: 0,
    posts: 0,
    change: 0,
    ...overrides,
  };
}

describe('PlatformPerformanceCard', () => {
  it('uniqueViewersShare() computes each platform\'s share of total UniqueViewers (not Reach)', () => {
    const fixture = TestBed.createComponent(PlatformPerformanceCard);
    const platforms: PlatformStat[] = [
      stat({ key: 'instagram', uniqueViewers: 300 }),
      stat({ key: 'facebook', uniqueViewers: 100 }),
    ];
    fixture.componentRef.setInput('platforms', platforms);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      uniqueViewersShare(p: PlatformStat): number;
    };
    expect(component.uniqueViewersShare(platforms[0])).toBe(75);
    expect(component.uniqueViewersShare(platforms[1])).toBe(25);
  });

  it('uniqueViewersShare() returns 0 for every platform when total UniqueViewers is 0', () => {
    const fixture = TestBed.createComponent(PlatformPerformanceCard);
    const platforms: PlatformStat[] = [stat({ uniqueViewers: 0 })];
    fixture.componentRef.setInput('platforms', platforms);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      uniqueViewersShare(p: PlatformStat): number;
    };
    expect(component.uniqueViewersShare(platforms[0])).toBe(0);
  });
});
