import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { NotificationSummary } from '../model/notification.model';
import { AuthService } from '../core/auth/auth.service';

/** Payload shape of the "notificationReceived" SignalR event — mirrors the backend's
 *  NotificationCreatedPayload (Rawaj.Application.Common.Policies.NotificationPublisher). */
interface NotificationCreatedEvent {
  notificationId: string;
  userId: string;
  type: NotificationSummary['type'];
  category: NotificationSummary['category'];
  title: string;
  message: string;
  refId: string | null;
  refType: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;
  /** The API's base origin without the "/api/v1" REST prefix — SignalR hubs live at the app root. */
  private readonly hubUrl = `${environment.apiUrl.replace(/\/api\/v\d+\/?$/, '')}/hubs/notifications`;

  private readonly _items = signal<NotificationSummary[]>([]);
  private readonly _unreadCount = signal(0);
  private readonly _loading = signal(false);

  readonly items = this._items.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  /** Backs the header dropdown — the 5 newest, unfiltered. */
  readonly recent = computed(() => this._items().slice(0, 5));

  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private hubConnection: HubConnection | null = null;

  refresh(page = 1, pageSize = 30): Observable<ApiResponse<PagedResult<NotificationSummary>>> {
    this._loading.set(true);
    return this.http
      .get<ApiResponse<PagedResult<NotificationSummary>>>(`${this.baseUrl}?page=${page}&pageSize=${pageSize}`)
      .pipe(
        tap({
          next: res => {
            this._loading.set(false);
            if (res.data) this._items.set(res.data.items);
          },
          error: () => this._loading.set(false),
        }),
      );
  }

  /** Cheap poll: reads only PagedResult.totalCount with a 1-row payload, so the badge never
   *  under-counts past whatever page the dropdown happens to have loaded. */
  refreshUnreadCount(): Observable<number> {
    return this.http
      .get<ApiResponse<PagedResult<NotificationSummary>>>(`${this.baseUrl}?unreadOnly=true&page=1&pageSize=1`)
      .pipe(
        map(res => res.data?.totalCount ?? 0),
        tap(count => this._unreadCount.set(count)),
      );
  }

  markRead(id: string): Observable<ApiResponse<boolean>> {
    const before = this._items();
    const wasUnread = before.find(n => n.id === id)?.isRead === false;

    this._items.update(list => list.map(n => (n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n)));
    if (wasUnread) this._unreadCount.update(c => Math.max(0, c - 1));

    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/${id}/read`, {}).pipe(
      tap({
        error: () => {
          // Roll back on failure.
          this._items.set(before);
          if (wasUnread) this._unreadCount.update(c => c + 1);
        },
      }),
    );
  }

  markAllRead(): Observable<ApiResponse<number>> {
    const before = this._items();
    const beforeCount = this._unreadCount();

    this._items.update(list => list.map(n => (n.isRead ? n : { ...n, isRead: true, readAt: new Date().toISOString() })));
    this._unreadCount.set(0);

    return this.http.post<ApiResponse<number>>(`${this.baseUrl}/read-all`, {}).pipe(
      tap({
        error: () => {
          this._items.set(before);
          this._unreadCount.set(beforeCount);
        },
      }),
    );
  }

  /** Dashboard-scoped polling — call from user-layout, not an APP_INITIALIZER, so anonymous
   *  visitors on public pages never poll and 401. Skips ticks while the tab is hidden. Also opens
   *  the real-time SignalR connection (see connectRealtime) — this is now purely a reconciliation
   *  safety net for missed pushes (dropped socket, etc.), so the interval is much longer than the
   *  pre-SignalR 60s default; it "never under-counts", it just may take longer to notice a miss. */
  startPolling(intervalMs = 180_000): void {
    this.stopPolling();
    this.refreshUnreadCount().subscribe();
    this.pollHandle = setInterval(() => {
      if (document.hidden) return;
      this.refreshUnreadCount().subscribe();
    }, intervalMs);
    this.connectRealtime();
  }

  stopPolling(): void {
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  /** Opens the SignalR connection and starts listening for "notificationReceived" pushes. Safe to
   *  call more than once — a no-op if already connected/connecting. Connection failures (offline,
   *  server down) are swallowed here; the reduced-frequency polling in startPolling() covers for it. */
  connectRealtime(): void {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) return;

    const connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => this.authService.accessToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    connection.on('notificationReceived', (payload: NotificationCreatedEvent) => this.handleRealtimeNotification(payload));

    connection.start().catch(() => {
      // No real-time push for this session — the polling fallback keeps the badge from going
      // stale for more than a few minutes.
    });

    this.hubConnection = connection;
  }

  disconnectRealtime(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
  }

  private handleRealtimeNotification(payload: NotificationCreatedEvent): void {
    if (this._items().some(n => n.id === payload.notificationId)) return; // already have it (reconnect race)

    const notification: NotificationSummary = {
      id: payload.notificationId,
      type: payload.type,
      category: payload.category,
      title: payload.title,
      message: payload.message,
      refId: payload.refId,
      refType: payload.refType,
      isRead: false,
      readAt: null,
      createdAt: payload.createdAt,
    };

    this._items.update(list => [notification, ...list]);
    this._unreadCount.update(c => c + 1);
  }

  /** Notifications are per-USER, not per-tenant (the backend scopes them by the signed-in user
   *  id, not by X-Tenant-Id) — do NOT call this from switchTenant(). Only from logout. */
  clear(): void {
    this.stopPolling();
    this.disconnectRealtime();
    this._items.set([]);
    this._unreadCount.set(0);
  }
}
