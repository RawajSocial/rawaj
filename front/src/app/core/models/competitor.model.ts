import { CompetitorStatus } from './enums';

export interface CompetitorSummary {
  competitorId: string;
  name: string;
  url: string | null;
  ragged: boolean;
  status: CompetitorStatus;
  lastScrapedAt: string | null;
}

export interface AddCompetitorRequest {
  brandProfileId: string;
  name: string;
  url?: string | null;
  socialHandles?: Record<string, string> | null;
  notes?: string | null;
}

export interface AddCompetitorResponse {
  competitorId: string;
  brandProfileId: string;
  name: string;
}

export interface AnalyzeCompetitorSource {
  title: string;
  url: string;
}

export interface AnalyzeCompetitorResponse {
  competitorId: string;
  summary: string | null;
  sources: AnalyzeCompetitorSource[];
}

export interface ScrapeWebsiteResponse {
  competitorId: string;
  ragDocumentId: string;
  title: string | null;
  textContent: string;
}

export interface RagDocumentSummary {
  ragDocumentId: string;
  sourceUrl: string | null;
  competitorsData: string | null;
  indexedAt: string | null;
}
