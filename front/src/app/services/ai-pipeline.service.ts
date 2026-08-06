import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import {
  AiArtifactKind, AiPipelineStageKind, ApproveStrategyResponse, CancelRunResponse,
  GetArtifactResponse, GetRunStatusResponse, RefineStrategyResponse, ResumeRunResponse,
  RunStageResponse, StartRunResponse, TERMINAL_RUN_STATUSES,
} from '../model/ai-pipeline.model';

/** Talks to `/ai-pipeline` and holds the polled status of whichever run a caller is currently
 *  watching. One run at a time — a campaign only ever has one active run, and the strategy and
 *  content pages (C21/C22) each watch their own campaign's run independently, so there is no need
 *  for this service to track more than one at once. */
@Injectable({ providedIn: 'root' })
export class AiPipelineService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/ai-pipeline`;

  private readonly _run = signal<GetRunStatusResponse | null>(null);
  readonly run = this._run.asReadonly();
  readonly isTerminal = computed(() => {
    const run = this._run();
    return !!run && TERMINAL_RUN_STATUSES.has(run.status);
  });

  private pollHandle: ReturnType<typeof setInterval> | null = null;

  start(campaignId: string): Observable<ApiResponse<StartRunResponse>> {
    return this.http.post<ApiResponse<StartRunResponse>>(`${this.baseUrl}/campaigns/${campaignId}/start`, {});
  }

  getStatus(runId: string): Observable<ApiResponse<GetRunStatusResponse>> {
    return this.http.get<ApiResponse<GetRunStatusResponse>>(`${this.baseUrl}/runs/${runId}`);
  }

  getArtifact(campaignId: string, kind: AiArtifactKind): Observable<ApiResponse<GetArtifactResponse>> {
    return this.http.get<ApiResponse<GetArtifactResponse>>(`${this.baseUrl}/campaigns/${campaignId}/artifacts/${kind}`);
  }

  runStage(runId: string, kind: AiPipelineStageKind): Observable<ApiResponse<RunStageResponse>> {
    return this.http.post<ApiResponse<RunStageResponse>>(`${this.baseUrl}/runs/${runId}/stages/${kind}/run`, {});
  }

  resume(runId: string): Observable<ApiResponse<ResumeRunResponse>> {
    return this.http.post<ApiResponse<ResumeRunResponse>>(`${this.baseUrl}/runs/${runId}/resume`, {});
  }

  cancel(runId: string): Observable<ApiResponse<CancelRunResponse>> {
    return this.http.post<ApiResponse<CancelRunResponse>>(`${this.baseUrl}/runs/${runId}/cancel`, {});
  }

  approve(runId: string): Observable<ApiResponse<ApproveStrategyResponse>> {
    return this.http.post<ApiResponse<ApproveStrategyResponse>>(`${this.baseUrl}/runs/${runId}/approve`, {});
  }

  refine(campaignId: string, feedback: string): Observable<ApiResponse<RefineStrategyResponse>> {
    return this.http.post<ApiResponse<RefineStrategyResponse>>(`${this.baseUrl}/campaigns/${campaignId}/refine`, { feedback });
  }

  /** Polls `GetRunStatus` every `intervalMs`, updating `run()`, until the run reaches a terminal
   *  state (see `isTerminal`) or `stopPolling()`/`clear()` is called — the replacement for the
   *  client-side `runPipeline`/`runDiagnosis`/`runStrategy` callback chain and the simulated content
   *  progress bar alike (C21/C22 wire this up; this service only drives the signal). Skips ticks
   *  while the tab is hidden, matching `NotificationService.startPolling`. A failed tick is
   *  swallowed rather than stopping the poll — a transient network blip should self-heal on the next
   *  tick, not strand the page on a stale status. */
  startPolling(runId: string, intervalMs = 2000): void {
    this.stopPolling();
    this.pollOnce(runId);
    this.pollHandle = setInterval(() => {
      if (document.hidden) return;
      this.pollOnce(runId);
    }, intervalMs);
  }

  stopPolling(): void {
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  /** Stops polling and drops the held status — call when navigating away from the campaign being
   *  watched, so a later page doesn't briefly render a previous campaign's run. */
  clear(): void {
    this.stopPolling();
    this._run.set(null);
  }

  private pollOnce(runId: string): void {
    this.getStatus(runId).subscribe(res => {
      if (!res.data) return;
      this._run.set(res.data);
      if (TERMINAL_RUN_STATUSES.has(res.data.status)) this.stopPolling();
    });
  }
}
