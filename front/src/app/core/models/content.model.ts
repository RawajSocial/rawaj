import { ContentStatus, ContentType, Language, SocialPlatform, VisualAssetType } from './enums';

export interface ContentItemSummary {
  contentItemId: string;
  contentType: ContentType;
  platform: SocialPlatform;
  language: Language;
  content: string;
  status: ContentStatus;
  createdAt: string;
}

export interface ContentItemDetail {
  contentItemId: string;
  campaignId: string | null;
  contentType: ContentType;
  platform: SocialPlatform;
  language: Language;
  title: string | null;
  content: string;
  hashtags: string[];
  cta: string | null;
  tone: string | null;
  status: ContentStatus;
  reviewedBy: string | null;
  reviewedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface GenerateContentItemRequest {
  campaignId: string;
  contentType: ContentType;
  platform: SocialPlatform;
  language: Language;
  tone?: string | null;
  additionalInstructions?: string | null;
}

export interface GenerateContentItemResponse {
  contentItemId: string;
  campaignId: string;
  content: string;
  status: ContentStatus;
}

export interface RegenerateContentItemResponse {
  contentItemId: string;
  content: string;
  status: ContentStatus;
  revisionNumber: number;
}

export interface ContentRevisionSummary {
  id: string;
  revisionNumber: number;
  revisionPrompt: string;
  previous: string;
  current: string;
  revisedBy: string;
  createdAt: string;
}

export interface VisualAssetSummary {
  visualAssetId: string;
  contentItemId: string | null;
  type: VisualAssetType;
  fileUrl: string;
  isApproved: boolean;
  createdAt: string;
}

export interface GenerateVisualAssetRequest {
  campaignId: string;
  contentItemId?: string | null;
  type: VisualAssetType;
  prompt: string;
}

export interface GenerateVisualAssetResponse {
  visualAssetId: string;
  campaignId: string;
  fileUrl: string;
}
