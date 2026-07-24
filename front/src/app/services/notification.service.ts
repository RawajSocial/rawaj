import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../model/auth.model';
import { PagedResult } from '../model/paged-result.model';
import { NotificationSummary } from '../model/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  private readonly _items = signal<NotificationSummary[]>([]);
  private readonly _unreadCount = signal(0);
  private readonly _loading = signal(false);

  readonly items = this._items.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  /** Backs the header dropdown — the 5 newest, unfiltered. */
  readonly recent = computed(() => this._items().slice(0, 5));

  private pollHandle: ReturnType<typeof setInterval> | null = null;

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
   *  visitors on public pages never poll and 401. Skips ticks while the tab is hidden. */
  startPolling(intervalMs = 60_000): void {
    this.stopPolling();
    this.refreshUnreadCount().subscribe();
    this.pollHandle = setInterval(() => {
      if (document.hidden) return;
      this.refreshUnreadCount().subscribe();
    }, intervalMs);
  }

  stopPolling(): void {
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  /** Notifications are per-USER, not per-tenant (the backend scopes them by the signed-in user
   *  id, not by X-Tenant-Id) — do NOT call this from switchTenant(). Only from logout. */
  clear(): void {
    this.stopPolling();
    this._items.set([]);
    this._unreadCount.set(0);
  }
}
