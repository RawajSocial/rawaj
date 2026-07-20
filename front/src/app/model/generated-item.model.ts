export type GenType     = 'static-ad' | 'video' | 'text';
export type GenStatus   = 'generating' | 'generated' | 'failed';
export type AdSize      = 'square' | 'portrait' | 'landscape' | 'story';
export type ContentTone = 'professional' | 'casual' | 'energetic' | 'luxurious';
export type TextType    = 'caption' | 'hashtags' | 'ad-copy' | 'blog';

export interface GeneratedAsset {
  name: string;
  url: string;
}

export interface GeneratedItem {
  id: string;
  type: GenType;
  title: string;
  brand: string;
  status: GenStatus;
  createdAt: string;
  size?: AdSize;
  tone?: ContentTone;
  language?: string;
  description?: string;
  textContent?: string;
  thumbnailUrl?: string;
  videoUrl?: string;
  /** Which brand profile / campaign this content was generated for — lets
   *  the media library (إعلاناتي) filter generated content by either. */
  brandProfileId?: string;
  campaignId?: string;
  /** Reference assets the user attached to the generation request. */
  assets?: GeneratedAsset[];
}

export const TYPE_CFG: Record<GenType, { label: string; desc: string; icon: string; color: string }> = {
  'static-ad': { label: 'إعلان ثابت',   desc: 'تصاميم إعلانية احترافية بالذكاء الاصطناعي', icon: 'fa-solid fa-image',        color: '#e91e8c' },
  'video':     { label: 'فيديو',          desc: 'مقاطع فيديو ترويجية مُنشأة تلقائياً',       icon: 'fa-solid fa-film',          color: '#f97316' },
  'text':      { label: 'محتوى نصي',     desc: 'تعليقات وهاشتاقات ونصوص إعلانية',           icon: 'fa-solid fa-align-right',   color: '#7C3AED' },
};

export const SIZE_CFG: Record<AdSize, { label: string; ratio: string }> = {
  square:    { label: 'مربع (1:1)',    ratio: '1080×1080' },
  portrait:  { label: 'عمودي (4:5)',  ratio: '1080×1350' },
  landscape: { label: 'أفقي (16:9)', ratio: '1920×1080' },
  story:     { label: 'ستوري (9:16)', ratio: '1080×1920' },
};

export const TONE_CFG: Record<ContentTone, string> = {
  professional: 'احترافي',
  casual:       'غير رسمي',
  energetic:    'نشيط ومتحمس',
  luxurious:    'فاخر وراقي',
};

export const TEXT_TYPE_CFG: Record<TextType, string> = {
  caption:   'تعليق منشور',
  hashtags:  'هاشتاقات',
  'ad-copy': 'نص إعلاني',
  blog:      'منشور مدونة',
};
