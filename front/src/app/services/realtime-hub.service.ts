import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from '../core/auth/auth.service';

/** The one SignalR connection to `/hubs/notifications`, shared by every service that needs a
 *  real-time push (notifications, AI pipeline status, ...). A hub connection carries as many event
 *  subscriptions as callers need — there was never a reason for each service to open its own
 *  connection to the same hub, which used to double negotiate/connect/keep-alive traffic. */
@Injectable({ providedIn: 'root' })
export class RealtimeHubService {
  private readonly authService = inject(AuthService);
  /** The API's base origin without the "/api/v1" REST prefix — SignalR hubs live at the app root. */
  private readonly hubUrl = `${environment.apiUrl.replace(/\/api\/v\d+\/?$/, '')}/hubs/notifications`;

  private connection: HubConnection | null = null;
  private refCount = 0;
  private readonly pendingHandlers: Array<{ eventName: string; handler: (payload: unknown) => void }> = [];

  /** Opens the shared connection if this is the first caller since the last full release;
   *  otherwise just counts this caller in. Balance every `connect()` with a `release()` (e.g. in
   *  the calling service's `clear()`/teardown) so the socket actually closes once nobody needs it. */
  connect(): void {
    this.refCount++;
    if (this.connection) return;

    const connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => this.authService.accessToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    for (const { eventName, handler } of this.pendingHandlers) {
      connection.on(eventName, handler);
    }

    connection.start().catch(() => {
      // No real-time push for this session — callers' own polling fallback covers for it.
    });

    this.connection = connection;
  }

  /** Balances a `connect()` call. Stops the underlying connection once the last caller has
   *  released it. */
  release(): void {
    this.refCount = Math.max(0, this.refCount - 1);
    if (this.refCount === 0 && this.connection) {
      this.connection.stop();
      this.connection = null;
    }
  }

  /** Registers a handler for a hub event, deduplicated by (eventName, handler reference) so a
   *  caller can call this more than once (e.g. across a disconnect/reconnect cycle) with a stable
   *  bound method without double-registering. Safe to call before `connect()` — the handler is
   *  queued and attached to whichever connection instance gets created next. */
  on<T>(eventName: string, handler: (payload: T) => void): void {
    const asUnknown = handler as (payload: unknown) => void;
    const alreadyQueued = this.pendingHandlers.some(h => h.eventName === eventName && h.handler === asUnknown);
    if (!alreadyQueued) {
      this.pendingHandlers.push({ eventName, handler: asUnknown });
    }
    this.connection?.on(eventName, asUnknown);
  }
}
