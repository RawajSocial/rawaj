/** GET /api/v1/visual-assets — Rawaj.Application.Features.VisualAssets.GetVisualAssets.VisualAssetSummary */
export interface VisualAssetSummary {
  visualAssetId: string;
  contentItemId?: string | null;
  type: 'Image' | 'Banner' | 'Logo' | 'Story' | 'Ad' | 'VideoThumbnail';
  fileUrl: string;
  isApproved: boolean;
  createdAt: string;
}

/** POST /api/v1/visual-assets/generate request body — GenerateVisualAssetCommand. */
export interface GenerateVisualAssetInput {
  brandProfileId: string;
  campaignId?: string;
  contentItemId?: string;
  type: VisualAssetSummary['type'];
  prompt: string;
}

/** POST /api/v1/visual-assets/generate — GenerateVisualAssetResponse */
export interface GenerateVisualAssetResponse {
  visualAssetId: string;
  campaignId?: string | null;
  fileUrl: string;
}
