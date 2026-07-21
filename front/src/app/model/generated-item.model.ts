import { ContentStatus, ContentType, VisualAssetType } from '../core/models';

export type GenType     = 'static-ad' | 'video' | 'text';
export type GenStatus   = 'generating' | 'generated' | 'failed';
export type AdSize      = 'square' | 'portrait' | 'landscape' | 'story';
export type ContentTone = 'professional' | 'casual' | 'energetic' | 'luxurious';
export type TextType    = 'caption' | 'hashtags' | 'post' | 'story' | 'reel-script' | 'ad-copy' | 'blog';

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
  /** Real backend review status - only meaningful for `type === 'text'` (content items). Content
   * must be Approved before it can be scheduled (enforced server-side too). */
  reviewStatus?: ContentStatus;
  /** Real backend approval flag - only meaningful for `type === 'static-ad'` (visual assets). */
  isApproved?: boolean;
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
  caption:      'تعليق منشور',
  hashtags:     'هاشتاقات',
  post:         'منشور',
  story:        'نص ستوري',
  'reel-script': 'نص ريلز',
  'ad-copy':    'نص إعلاني',
  blog:         'منشور مدونة',
};

export const TEXT_TYPE_TO_CONTENT_TYPE: Record<TextType, ContentType> = {
  caption: 'Caption',
  hashtags: 'Caption',
  post: 'Post',
  story: 'Story',
  'reel-script': 'ReelScript',
  'ad-copy': 'AdCopy',
  blog: 'Blog',
};

export const VISUAL_TYPE_CFG: Record<VisualAssetType, string> = {
  Image: 'صورة',
  Banner: 'بانر',
  Logo: 'شعار',
  Story: 'صورة ستوري',
  Ad: 'صورة إعلانية',
  VideoThumbnail: 'صورة مصغرة للفيديو',
};
