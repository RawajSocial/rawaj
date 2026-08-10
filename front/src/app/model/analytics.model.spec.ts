import {
  formatEngagementRate, metricAvailable, PostAnalyticsSnapshot,
  computeMetricGrowth, comparePostToCampaignAverage,
} from './analytics.model';

function snapshot(overrides: Partial<PostAnalyticsSnapshot>): PostAnalyticsSnapshot {
  return {
    id: 's1',
    scheduledPostId: 'p1',
    platform: 'Facebook',
    recordedAt: '2026-01-01T00:00:00Z',
    views: null,
    uniqueViewers: null,
    likes: null,
    comments: null,
    shares: null,
    saves: null,
    clicks: null,
    engagementRate: null,
    ...overrides,
  };
}

describe('metricAvailable', () => {
  it('is true for "views" when at least one snapshot reports a non-null views value', () => {
    const posts = [snapshot({ views: null }), snapshot({ views: 100 })];
    expect(metricAvailable(posts, 'views')).toBe(true);
  });

  it('is true for "uniqueViewers" when at least one snapshot reports a non-null value', () => {
    const posts = [snapshot({ uniqueViewers: null }), snapshot({ uniqueViewers: 42 })];
    expect(metricAvailable(posts, 'uniqueViewers')).toBe(true);
  });

  it('is false when every snapshot has a null value for the metric', () => {
    const posts = [snapshot({ views: null }), snapshot({ views: null })];
    expect(metricAvailable(posts, 'views')).toBe(false);
  });

  it('is false for an empty snapshot list', () => {
    expect(metricAvailable([], 'engagementRate')).toBe(false);
  });
});

describe('formatEngagementRate', () => {
  it('scales a fraction from the API into a whole percentage (0.1 -> "10%", not "0.1%")', () => {
    expect(formatEngagementRate(0.1)).toBe('10%');
  });

  it('rounds to one decimal place', () => {
    expect(formatEngagementRate(0.0834)).toBe('8.3%');
  });

  it('handles a 100% rate', () => {
    expect(formatEngagementRate(1)).toBe('100%');
  });

  it('handles a zero rate', () => {
    expect(formatEngagementRate(0)).toBe('0%');
  });

  it('returns an em dash for null/undefined instead of "null%"', () => {
    expect(formatEngagementRate(null)).toBe('—');
    expect(formatEngagementRate(undefined)).toBe('—');
  });
});

describe('computeMetricGrowth', () => {
  it('computes a positive delta and percent change between two snapshots', () => {
    expect(computeMetricGrowth(150, 100)).toEqual({ delta: 50, percentChange: 50 });
  });

  it('computes a negative delta when the metric decreased', () => {
    expect(computeMetricGrowth(80, 100)).toEqual({ delta: -20, percentChange: -20 });
  });

  it('returns a null percentChange when the prior value is 0 (division by zero has no meaning)', () => {
    expect(computeMetricGrowth(10, 0)).toEqual({ delta: 10, percentChange: null });
  });

  it('returns null delta/percentChange when either value is missing (unavailable metric)', () => {
    expect(computeMetricGrowth(null, 100)).toEqual({ delta: null, percentChange: null });
    expect(computeMetricGrowth(100, null)).toEqual({ delta: null, percentChange: null });
    expect(computeMetricGrowth(undefined, undefined)).toEqual({ delta: null, percentChange: null });
  });

  it('rounds percentChange to one decimal place', () => {
    expect(computeMetricGrowth(110, 90)).toEqual({ delta: 20, percentChange: 22.2 });
  });
});

describe('comparePostToCampaignAverage', () => {
  it('computes a positive percentage-point delta when the post outperforms the campaign average', () => {
    expect(comparePostToCampaignAverage(0.12, 0.08)).toEqual({ postRate: 0.12, campaignRate: 0.08, deltaPoints: 4 });
  });

  it('computes a negative percentage-point delta when the post underperforms the campaign average', () => {
    expect(comparePostToCampaignAverage(0.05, 0.08)).toEqual({ postRate: 0.05, campaignRate: 0.08, deltaPoints: -3 });
  });

  it('returns a null deltaPoints when either rate is unavailable, without discarding whichever rate is known', () => {
    expect(comparePostToCampaignAverage(null, 0.08)).toEqual({ postRate: null, campaignRate: 0.08, deltaPoints: null });
    expect(comparePostToCampaignAverage(0.08, undefined)).toEqual({ postRate: 0.08, campaignRate: null, deltaPoints: null });
  });
});
