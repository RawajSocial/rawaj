/** GET /api/v1/visual-assets — Rawaj.Application.Features.VisualAssets.GetVisualAssets.VisualAssetSummary */
export interface VisualAssetSummary {
  visualAssetId: string;
  contentItemId?: string | null;
  type: 'Image' | 'Banner' | 'Logo' | 'Story' | 'Ad' | 'VideoThumbnail';
  fileUrl: string;
  isApproved: boolean;
  createdAt: string;
}
