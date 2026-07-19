import { Injectable, computed, signal } from '@angular/core';

export type TenantAccountType = 'agency' | 'business';

/**
 * Who's using the platform shapes how social accounts attach to brand
 * profiles: an agency manages several client brands, each with its own
 * connected social accounts, while a single business owner has exactly one
 * brand profile sharing one set of connections.
 *
 * TODO: replace with the real tenant/session lookup once auth exists — this
 * mirrors the `accountType` chosen during onboarding (see
 * `setup-type-selector`).
 */
@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly _accountType = signal<TenantAccountType>('agency');
  readonly accountType = this._accountType.asReadonly();

  readonly isAgency = computed(() => this._accountType() === 'agency');
}
