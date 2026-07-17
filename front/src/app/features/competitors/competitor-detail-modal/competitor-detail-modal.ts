import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { ModalShell } from '../../../shared/components/modal-shell/modal-shell';
import { CompetitorsApiService } from '../../../core/api/competitors-api.service';
import { ApiError } from '../../../core/api';
import {
  AnalyzeCompetitorResponse,
  CompetitorSummary,
  RagDocumentSummary,
  ScrapeWebsiteResponse,
} from '../../../core/models';

@Component({
  selector: 'app-competitor-detail-modal',
  imports: [ModalShell],
  templateUrl: './competitor-detail-modal.html',
  styleUrl: './competitor-detail-modal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompetitorDetailModal {
  readonly open = input(false);
  readonly competitor = input<CompetitorSummary | null>(null);

  readonly closed = output<void>();
  readonly analyzed = output<void>();

  private readonly competitorsApi = inject(CompetitorsApiService);

  protected readonly history = signal<RagDocumentSummary[]>([]);
  protected readonly loadingHistory = signal(false);

  protected readonly analyzing = signal(false);
  protected readonly analyzeResult = signal<AnalyzeCompetitorResponse | null>(null);
  protected readonly analyzeError = signal<string | null>(null);

  protected readonly scraping = signal(false);
  protected readonly scrapeResult = signal<ScrapeWebsiteResponse | null>(null);
  protected readonly scrapeError = signal<string | null>(null);

  constructor() {
    effect(() => {
      const competitor = this.competitor();
      if (this.open() && competitor) {
        this.analyzeResult.set(null);
        this.analyzeError.set(null);
        this.scrapeResult.set(null);
        this.scrapeError.set(null);
        this.loadHistory(competitor.competitorId);
      }
    });
  }

  private loadHistory(competitorId: string): void {
    this.loadingHistory.set(true);
    this.competitorsApi.getAnalysis(competitorId).subscribe({
      next: (history) => {
        this.history.set(history);
        this.loadingHistory.set(false);
      },
      error: () => this.loadingHistory.set(false),
    });
  }

  protected analyze(): void {
    const competitor = this.competitor();
    if (!competitor) return;

    this.analyzing.set(true);
    this.analyzeError.set(null);

    this.competitorsApi.analyze(competitor.competitorId).subscribe({
      next: (result) => {
        this.analyzing.set(false);
        this.analyzeResult.set(result);
        this.loadHistory(competitor.competitorId);
        this.analyzed.emit();
      },
      error: (error: unknown) => {
        this.analyzing.set(false);
        this.analyzeError.set(error instanceof ApiError ? error.message : 'تعذر تحليل المنافس، حاول مرة أخرى.');
      },
    });
  }

  protected scrape(): void {
    const competitor = this.competitor();
    if (!competitor?.url) return;

    this.scraping.set(true);
    this.scrapeError.set(null);

    this.competitorsApi.scrapeWebsite(competitor.competitorId, competitor.url).subscribe({
      next: (result) => {
        this.scraping.set(false);
        this.scrapeResult.set(result);
        this.loadHistory(competitor.competitorId);
        this.analyzed.emit();
      },
      error: (error: unknown) => {
        this.scraping.set(false);
        this.scrapeError.set(error instanceof ApiError ? error.message : 'تعذر استخراج بيانات الموقع، حاول مرة أخرى.');
      },
    });
  }

  protected formatDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  protected requestClose(): void {
    this.closed.emit();
  }
}
