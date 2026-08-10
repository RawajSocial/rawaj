import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { RealtimeHubService } from './realtime-hub.service';
import {
  AiArtifactKind, AiPipelineStageKind, ApproveStrategyResponse, CancelRunResponse,
  GetArtifactResponse, GetRunStatusResponse, RefineStrategyResponse, ResumeRunResponse,
  RunStageResponse, StartRunResponse, TERMINAL_RUN_STATUSES,
} from '../model/ai-pipeline.model';

/** Payload shape of the "pipelineRunUpdated" SignalR event — mirrors the backend's
 *  PipelineRunUpdatedPayload (Rawaj.Application.Common.Policies.PipelineRunPublisher). Field-for-field
 *  compatible with GetRunStatusResponse (plus userId), so a pushed update can replace a polled one
 *  without translation. */
type PipelineRunUpdatedEvent = GetRunStatusResponse & { userId: string };

/** Talks to `/ai-pipeline` and holds the status of whichever run a caller is currently watching.
 *  One run at a time — a campaign only ever has one active run, and the strategy and content pages
 *  (C21/C22) each watch their own campaign's run independently, so there is no need for this service
 *  to track more than one at once.
 *
 *  Status updates arrive primarily over SignalR ("pipelineRunUpdated", pushed by the backend the
 *  moment PipelineOrchestrator advances a run) — HTTP polling is kept only as a low-frequency
 *  reconciliation safety net for a dropped socket, matching NotificationService's pattern. */
@Injectable({ providedIn: 'root' })
export class AiPipelineService {
  private readonly http = inject(HttpClient);
  private readonly hub = inject(RealtimeHubService);
  private readonly baseUrl = `${environment.apiUrl}/ai-pipeline`;

  private readonly _run = signal<GetRunStatusResponse | null>(null);
  readonly run = this._run.asReadonly();
  readonly isTerminal = computed(() => {
    const run = this._run();
    return !!run && TERMINAL_RUN_STATUSES.has(run.status);
  });

  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private realtimeConnected = false;
  private watchedRunId: string | null = null;
  /** Stable reference so RealtimeHubService can dedupe repeated `on()` registrations across a
   *  disconnect/reconnect cycle (this service connects/releases on every campaign page visit). */
  private readonly onPipelineRunUpdated = (payload: PipelineRunUpdatedEvent) => this.handleRealtimeUpdate(payload);

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

  /** Fetches the run once immediately, then relies on the "pipelineRunUpdated" SignalR push for
   *  further updates — the replacement for the client-side `runPipeline`/`runDiagnosis`/`runStrategy`
   *  callback chain and the simulated content progress bar alike (C21/C22 wire this up; this service
   *  only drives the signal). A slow `intervalMs` poll keeps running underneath as a reconciliation
   *  safety net for a dropped socket, matching `NotificationService.startPolling` — skips ticks while
   *  the tab is hidden. Stops on its own once the run reaches a terminal state (see `isTerminal`), or
   *  earlier via `stopPolling()`/`clear()`. A failed tick is swallowed rather than stopping the poll —
   *  a transient network blip should self-heal on the next tick, not strand the page on a stale
   *  status. */
  startPolling(runId: string, intervalMs = 20_000): void {
    this.stopPolling();
    this.watchedRunId = runId;
    this.pollOnce(runId);
    this.pollHandle = setInterval(() => {
      if (document.hidden) return;
      this.pollOnce(runId);
    }, intervalMs);
    this.connectRealtime();
  }

  stopPolling(): void {
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  /** Stops polling, closes the SignalR connection, and drops the held status — call when navigating
   *  away from the campaign being watched, so a later page doesn't briefly render a previous
   *  campaign's run. */
  clear(): void {
    this.stopPolling();
    this.disconnectRealtime();
    this.watchedRunId = null;
    this._run.set(null);
  }

  /** Joins the shared hub connection (the same one NotificationService pushes through) and starts
   *  listening for "pipelineRunUpdated". Safe to call more than once — a no-op if already joined.
   *  Connection failures (offline, server down) are swallowed by RealtimeHubService; the
   *  reduced-frequency polling in startPolling() covers for it. */
  private connectRealtime(): void {
    if (this.realtimeConnected) return;
    this.realtimeConnected = true;
    this.hub.connect();
    this.hub.on('pipelineRunUpdated', this.onPipelineRunUpdated);
  }

  private disconnectRealtime(): void {
    if (!this.realtimeConnected) return;
    this.realtimeConnected = false;
    this.hub.release();
  }

  private handleRealtimeUpdate(payload: PipelineRunUpdatedEvent): void {
    if (payload.runId !== this.watchedRunId) return; // an update for a run this tab isn't watching

    const { userId: _userId, ...run } = payload;
    this.applyRunUpdate(run);
  }

  private pollOnce(runId: string): void {
    this.getStatus(runId).subscribe(res => {
      if (!res.data) return;
      this.applyRunUpdate(res.data);
    });
  }

  /** The single place either delivery path (SignalR push or HTTP poll) is allowed to update `_run` —
   *  see `GetRunStatusResponse.version`'s remarks for why blindly trusting whichever one arrives last
   *  isn't safe. A same-or-newer version is applied normally; an older one is silently dropped, since
   *  the fresher state this tab already has is strictly more correct than reapplying a stale snapshot. */
  private applyRunUpdate(run: GetRunStatusResponse): void {
    const current = this._run();
    if (current && current.runId === run.runId && run.version < current.version) return;

    this._run.set(run);
    if (TERMINAL_RUN_STATUSES.has(run.status)) this.stopPolling();
  }
}
