import { formatEngagementRate, metricAvailable, PostAnalyticsSnapshot } from './analytics.model';

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
