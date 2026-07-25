/** GET /api/v1/visual-assets — Rawaj.Application.Features.VisualAssets.GetVisualAssets.VisualAssetSummary */
export interface VisualAssetSummary {
  visualAssetId: string;
  contentItemId?: string | null;
  type: 'Image' | 'Banner' | 'Logo' | 'Story' | 'Ad' | 'VideoThumbnail';
  fileUrl: string;
  isApproved: boolean;
  createdAt: string;
}

/** POST /api/v1/visual-assets/generate request body — GenerateVisualAssetCommand.
 *  `brandProfileId` is optional — omitting it produces a "standalone" generation not tied to any
 *  brand, for trying the product out before a brand profile exists. */
export interface GenerateVisualAssetInput {
  brandProfileId?: string;
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
