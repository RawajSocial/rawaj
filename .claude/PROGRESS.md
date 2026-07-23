# Rawaj — session progress log

Read this before starting a new session so nothing gets rebuilt or contradicted.
This is a running log of what's been done, what's real vs. mock, and what's
left — update it as work continues instead of trusting memory alone.

**Project**: Rawaj — an Arabic (RTL) AI-powered social-media marketing SaaS.
- Frontend: `front/` — Angular 21, standalone components, signals, Reactive Forms.
- Backend: `server/Rawaj/` — ASP.NET Core / .NET 10, CQRS via MediatR, EF Core, SQL Server.

See also: `front/.claude/CLAUDE.md` (Angular style rules) and
`front/.claude/README.md` (this project's specific frontend conventions —
error handling, loading states, styling, shared components). Follow both;
this file is the "what happened and what's next" log, not the style guide.

---

## Project structure — respect these conventions

### Frontend (`front/src/app/`)
- `layout/user/` — the tenant dashboard shell (`UserLayout`, `Sidebar`, `Header`), mounted at `/dashboard`.
- `layout/admin/` — the platform-admin shell (`AdminLayout`, `AdminSidebar`, `AdminHeader`, `admin-theme.css`), mounted at `/admin`. Dark-red/white palette lives only in the sidebar/header's own CSS — the content area intentionally uses the normal light theme (see "Admin dashboard" below).
- `features/<domain>/<page-name>/` — one folder per routed page or major sub-component, `.ts`/`.html`/`.css` per component. Dashboard-only pages that aren't part of the shell still live under `features/dashboard/*` (billing, settings, users, notifications, help-support, my-projects, loading-test-page).
- `features/admin/` — admin-only pages (`admin-overview-page`, `admin-users-page`, `admin-tenants-page`, `admin-plans-page`, `admin-settings-page`) + `admin-shared.css` (shared table/search/user-cell styles, mirrors `dashboard-shared.css`).
- `shared/components/` — reusable components used across features (`error-modal`, `modal-shell`, `page-loader`, `file-upload`, `faq-section`, `breadcrumb`, `page-header`).
- `shared/directives/` — `tooltip.directive.ts` (`appTooltip`), `gsap-reveal.directive.ts` (`appGsapReveal`).
- `core/auth/` — `AuthService`, `jwt.util.ts` (decode access token), `api-error.util.ts` (map failed HTTP responses to modal/form errors).
- `core/tenant/` — `TenantService` (real, HTTP-backed — replaced the old mock `services/tenant.service.ts`): current tenant summary signal (`coinBalance`, `isActivated`, `isAgency`, `defaultBrandProfileId`, plan/profile fields), `refresh()`/`updateProfile()`/`upgradeToAgency()`.
- `core/social/` — `SocialAccountService` (real, HTTP-backed): list/connect/disconnect social accounts for a brand profile via the backend's redirect-based OAuth flow.
- `core/guards/` — `auth.guard.ts`, `admin.guard.ts`, `guest.guard.ts` (functional guards, `CanActivateFn`/`CanMatchFn`).
- `core/interceptors/` — `auth.interceptor.ts` (attaches bearer token, one silent refresh-and-retry on 401).
- `model/` — plain interfaces/types, one file per domain (`auth.model.ts`, `tenant.model.ts`, `social-account.model.ts`, `campaign.model.ts`, `brand-profile.model.ts`, `admin.model.ts`, `generated-item.model.ts`, `team-member.model.ts`, etc.). Auth/tenant/social-account model types are named to mirror the backend's C# records exactly (see below).
- `services/` — several are still **mock data stores**: `private signal<T[]>` + `.asReadonly()` + `getById()` computed + plain mutation methods (`CampaignService`, `AdService`, `TeamMemberService`, `MediaService`, `AdminService`). Real, HTTP-backed exceptions: `AuthService`/`TenantService`/`SocialAccountService` (in `core/`) and `BrandProfileService` (in `services/`, now HTTP-backed — see item 12).
- `routes/` — `user.routes.ts` (children of `/dashboard`) and `admin.routes.ts` (children of `/admin`), composed by `app.routes.ts`.
- `environments/` — `environment.ts`/`.development.ts`/`.production.ts`, holds `apiUrl` (`https://localhost:7206/api/v1` in dev). Any new env-specific value goes here, in all three files.

### Backend (`server/Rawaj/`)
Layered/CQRS solution:
- `Rawaj.Domain` — entities, enums, value objects. No dependencies on other layers.
- `Rawaj.Application` — `Features/<Module>/<Action>/` folders, each holding a MediatR `Command`/`Query` + `Handler` + `Validator` (FluentValidation) + `Response` record. Common interfaces/models/policies live in `Application/Common/`.
- `Rawaj.Persistence` — `AppDbContext`, EF Core configurations, migrations, and the concrete implementations of `Application`'s interfaces (`IdentityService`, `BrandProfileService`, etc.).
- `Rawaj.Infrastructure` — cross-cutting concerns: JWT generation, encryption, storage, background jobs, external API clients.
- `Rawaj/` (API project) — `Controllers/*Controller.cs` (thin, just `sender.Send(command)` + map `Result` to `ApiResponse<T>`), `Program.cs`, `appsettings*.json`.
- Every controller action returns `Rawaj.Common.ApiResponse<T>`: `{ status: "success"|"fail"|"error", data, message, errors }` — `errors` is a `Dictionary<string,string[]>` (FluentValidation field errors), only populated on 400s.
- New backend work should follow this exact pattern (new `Features/<Module>/<Action>/` folder with all 4 files) — don't put logic directly in controllers.

---

## What's been built so far (chronological, high level)

1. **Ads/posts, campaigns**: renamed ads → "منشوراتك", redesigned `ad-card`/`campaign-card`, added campaign detail → calendar → post-detail page flow (real routes, no modals), shared mock services (`CampaignService`, `ScheduledPostService`, `AdService`).
2. **Landing page**: rebuilt "problems" section styling, hero/navbar glow tuning (dark-red/magenta palette, `--shadow-tag` design token).
3. **Shared infra**: `ErrorModalService` + `error-modal` component (built on `ModalShell`), `LoaderService` + `page-loader` (global, wired to router nav events, previewable at `/dashboard/loading-test`), 404 page, environment config files, `TooltipDirective` (`appTooltip`), generalized `FileUpload` component, redesigned `FaqSection` component.
4. **Users table / billing**: replaced role/department columns with project-count/credit-used, added ban/reactivate action, generalized the `--shadow-tag` box-shadow across badges/avatars/icons.
5. **Brand profiles**: `BrandProfile` model (mirrors backend `TenantBrandProfileResponse`), mock `BrandProfileService`, list page (tags-top/big-image/data-bottom/actions card), multi-step creation **page** (not modal) with progress bar, `Campaign.brandProfileId` linking campaigns to a brand profile.
6. **Settings**: social-account connections moved from a dashboard quick-action into Settings (agency accounts connect per brand profile via a new `TenantService`; business accounts share one connection set); removed the "المظهر" (appearance) tab.
7. **Content generation page**: reworked to a single output preview (placeholder → generating animation → result) instead of a full gallery, "edit with prompt" flow, assets upload with name+thumbnail previews, brand-profile/campaign tagging feeding new filters on the my-media library page.
8. **Layout reorg**: moved the dashboard shell out of `features/dashboard/` into `layout/user/` (renamed `Dashboard` → `UserLayout`); routes split into `routes/user.routes.ts` + `routes/admin.routes.ts`.
9. **Admin dashboard** (`/admin`, platform-wide, distinct from the tenant `/dashboard`): `AdminLayout`/`AdminSidebar`/`AdminHeader` (dark red + white text, hardcoded in their own CSS — **not** via the removed `.admin-shell` variable-override approach, which was tried and reverted because it also darkened the content area), pages for overview stats / cross-tenant users / tenants (agencies vs. business owners) / subscription plans / general settings, backed by a **mock** `AdminService`. The backend already has real equivalents (`Features/Admin/GetPlatformStats`, `GetUsers`, `GetTenants`, `SetUserActive`) that aren't wired up yet — see "Next steps."
10. **Real auth integration** (this is the first module connected to the live backend, not mock):
    - Fixed/confirmed `RawajDb` as the target database (both `appsettings.json` and `appsettings.Development.json` point to it); ran `dotnet ef database update` to create it locally (9 migrations applied).
    - Registered a real platform-admin account: `admin@gmail.com` / `Admin@123456789`, then set `IsPlatformAdmin = 1` directly via SQL (no API command exists for this yet).
    - Verified register → login → refresh-token → logout all work live against the local DB; confirmed the JWT's `platform_admin` claim reads `true` for that account.
    - Frontend: `model/auth.model.ts`, `core/auth/auth.service.ts` (+`jwt.util.ts`, `api-error.util.ts`), `core/interceptors/auth.interceptor.ts`, `core/guards/{auth,admin,guest}.guard.ts`, wired into `app.config.ts` (`provideHttpClient(withInterceptors(...))`) and `app.routes.ts` (`canActivate`/`canMatch` on `/dashboard`, `/admin`, `/account-setup`, `/on-boarding`, `guestGuard` on `/login`/`/sign-up`).
    - `login-form`/`sign-up-form` now call the real `AuthService`, show errors via `ErrorModalService`, map server field errors onto the form (`FormErrorsService` extended with a `serverMessage` error key), use `LoaderService` while in flight. Login's field was `emailOrUserName`; corrected to a proper `email` field since the backend only supports email login.
11. **Real tenant lifecycle** (auto-provisioning, coins, activation reward, agency upgrade, social OAuth — all real, not mock):
    - Backend: `Tenant` gained `CoinBalance` (int), `IsActivated` (bool), and a `TenantProfile` value object (JSON column, same pattern as `BrandInfo`: `Phone`, `Industry`, `Country`, `City`, `Website`, `AgencySize`, `ServicesOffered`) — migration `AddTenantCoinsActivationProfile`.
    - `RegisterCommandHandler` now auto-provisions a `Tenant` (`TenantType.Business`, 100 starting coins, `IsActivated=false`), a `Subscription` (Free plan, 14-day trial), a `TenantMember` (Owner), and **one default `TenantBrandProfile`** (`IsDefault=true`) — all via a new shared `Application/Common/Services/TenantProvisioningService`, also reused by the pre-existing (previously unused) `CreateTenantCommandHandler`.
    - `GetMyTenantQuery`/`Response` extended with `coinBalance`, `isActivated`, `planName`, `maxBrands`, `brandProfileCount`, `defaultBrandProfileId`, and the `TenantProfile` fields.
    - New `Features/Tenants/UpdateTenantProfile` (`PUT /tenants/me/profile`): merges the given fields into `TenantProfile`; when `Phone`+`Industry`+`Country`+`City` are all non-empty for the first time, flips `IsActivated=true` and grants **+50 coins** (`activationRewardGranted` in the response, guarded so it never re-grants).
    - New `Features/Tenants/UpgradeToAgency` (`POST /tenants/me/upgrade-to-agency`): requires `AgencySize`+`ServicesOffered` (Owner role only), fails if already `Agency`, otherwise sets `TenantType=Agency`, cancels the current `Subscription` and creates a new one on the **Pro** plan (already-seeded, `MaxBrands=5`) — no new plan entity needed.
    - `CreateBrandProfileCommandHandler`'s plan-limit failure message is now the exact sentinel string `"AGENCY_UPGRADE_REQUIRED"` (was a prose message) so the frontend can reliably detect "you need to upgrade" vs. any other failure. `BrandInfo`/`CreateBrandProfileCommand` also gained an optional `Location` field (JSON column, no migration needed).
    - All of the above verified live end-to-end via curl against `RawajDb` (register → tenant+default brand profile created with 100 coins → partial profile update doesn't reward → completing it rewards +50 coins once, not twice → blocked 2nd brand profile creation returns the sentinel → upgrade-to-agency flips type/plan → 2nd brand profile creation then succeeds).
    - Frontend: `core/tenant/tenant.service.ts` (replaces the old mock `services/tenant.service.ts`, same `isAgency` computed name so `settings-page` needed only an import-path change), `model/tenant.model.ts`. `login-form`/`sign-up-form` call `tenantService.refresh()` on success; `UserLayout`'s constructor also calls it once if the tenant signal is empty (covers page reload/deep-link). Sign-up now navigates straight to `/dashboard` (no more forced wizard) and dropped its now-dead `userName`/`businessName` fields.
    - `/account-setup` → renamed to `/upgrade-tenant`: the whole `features/account-setup/` folder was renamed to `features/upgrade-tenant/`, `setup-type-selector`/`business/*`/`otp-verification` deleted (a brand-new signup no longer needs to choose a type or verify OTP — that's all automatic now), keeping only the 5 agency steps + `setup-stepper`. The page now calls the real `upgradeToAgency()` and redirects into `/dashboard/brand-profiles/new` on success.
    - Dashboard home (`crm-page`) gained two quick-action cards: "اربط حسابات التواصل الاجتماعي" (→ new `/dashboard/social-accounts`) and "أكمل بياناتك واحصل على 50 كوين" (→ Settings profile tab; hides itself once `isActivated()`).
    - New `features/dashboard/social-accounts-page/` (real): lists/connects/disconnects social accounts for the tenant's default brand profile via `SocialAccountService`, using the backend's existing redirect-based OAuth flow (`GetAuthorizationUrl` → full-page redirect → backend's `Callback` action redirects back to `/dashboard/social-accounts?connected=...`/`?error=...`).
    - Settings profile tab gained a second card ("بيانات النشاط التجاري": phone/industry/country/city/website) wired to `tenantService.updateProfile()`, with a success modal on activation reward.
    - Billing page now shows the real `planName`/`coinBalance` from `TenantService` (invoices/usage bars stay mock).
    - Brand-profile creation page: added an optional `Location` field with the requested Arabic tip ("هذه المعلومة سوف تساعد رواج في تحليل المنافسين"); since brand-profiles remain a **mock** module this pass, the "upgrade required" gate on submit checks the mock service's local profile count + `tenantService.isAgency()` client-side (not the backend sentinel yet — that only takes effect once brand-profiles are wired to real HTTP).
12. **Brand profiles wired to real backend** (no longer mock):
    - `services/brand-profile.service.ts` now hits the live API — `refresh()` (`GET /brand-profiles`, maps `BrandProfileSummary` → `BrandProfile`), `create()` (`POST /brand-profiles`), `archive()` (`POST /brand-profiles/{id}/archive`), all funneled through a `mutateAndRefresh` helper that re-fetches the list on success so the `profiles` signal always mirrors server state. The old local `signal<BrandProfile[]>` mock store is gone.
    - The create wizard's "upgrade required" gate now reads the backend's real state via `tenantService` (`brandProfileCount()` / `maxBrands()` / `isAgency()`) instead of a client-side mock count.
13. **Brand-profile requirement gating** (new tenants start with **zero** brand profiles):
    - Backend: `RegisterCommandHandler` now provisions with `createDefaultBrandProfile: false` — a fresh signup gets a tenant + subscription + Owner member but **no** brand profile (reverses item 11's "one default `TenantBrandProfile`"). `CreateTenantCommandHandler` is untouched.
    - Frontend rule: navigation and page content stay **fully unrestricted** — every sidebar item is clickable, every route reachable, every page renders its normal layout/lists/cards. Only the brand-*dependent actions* are gated when `tenantService.brandProfileCount() === 0`, reusing the existing brand-cap pattern from `brand-profile-create-page.ts` inline (an `ErrorModalService.show(...)` warning + `router.navigate(['/dashboard/brand-profiles/new'])`) — no new shared component/directive.
    - Gated actions: campaigns-page (`startNewCampaign`, pause/resume), ads-page + ad-detail-page (`toggleStatus`), calendar-page (`savePost`/`deletePost`), marketing-plan-page (`goToOnboarding`/`remake`). Read-only detail/my-media pages were left alone (no mutating action to gate).
    - `crm-page` shows a "أنشئ ملف علامتك التجارية أولاً" CTA banner (reusing the reward-card style) while `brandProfileCount() === 0`; the header brand selector gains a third branch rendering an "أنشئ ملف علامة تجارية" link when the profiles list is empty.
    - **Content Generation is the sole exception** — it stays fully usable with zero brand profiles (already defaulted its brand id to `''`, no change needed).
14. **Brand-profile logos stored as files, not base64** (mirrors the user-avatar mechanism):
    - Previously the create wizard embedded the logo as a base64 `data:` URL in `BrandInfo.LogoUrl`. Now the file uploads to disk and only a short relative path is persisted.
    - Backend: generalized the avatar storage — `IAvatarStorageService`/`LocalAvatarStorageService` → `ILocalImageStorageService`/`LocalImageStorageService` with a `subfolder` param (`SaveAsync`/`DeleteAsync`; avatars keep their existing on-disk location); the two avatar handlers + DI registration were updated. New `Features/Brands/UploadBrandLogo/` (command/handler/response/validator, Admin-only, 5 MB cap, jpeg/png/webp/**svg**) + `POST /brand-profiles/logo` (accepts `IFormFile`, saves to `wwwroot/media/brandprofiles/logourls/`, returns `/media/brandprofiles/logourls/{file}`). The `LogoUrl` rule in the Create/Update brand-profile validators was relaxed to accept root-relative `/media/` paths (it required an absolute URI, which the new path would have failed; legacy `data:` URLs still pass).
    - Frontend: `BrandProfileService.uploadLogo(file)` (posts `FormData`), and `toBrandProfile` now resolves `logoUrl` through `resolveMediaUrl` (so `/media/...` paths resolve to the API origin in dev; legacy base64 values pass through unchanged). The wizard keeps the data URL for preview only, captures the real `File`, and on submit uploads it first then creates the profile with the returned path.
    - **Onboarding wizard's logo is out of scope** — it only ever stored the file *name*, never binary, so it wasn't touched.
15. **Dead auth nav links fixed**: the login page's "إنشاء حساب" link and the sign-up page's "تسجيل الدخول" link were `<a href="#">` placeholders with no routing. Added `RouterLink` to both standalone form components' `imports` and pointed them at `/sign-up` and `/login`.

## What's real vs. still mock

- **Real (hits the live API)**: register, login, refresh-token, logout, tenant auto-provisioning, `GetMyTenant`, `UpdateTenantProfile`, `UpgradeToAgency`, social-account connect/list/disconnect (OAuth), **brand profiles** (list/create/archive + logo upload; see items 12–14).
- **Everything else is still mock-data-driven** (local signals, no HTTP calls): campaigns, ads, the per-brand social-connection UI in the Settings "connections" tab (separate from the real `/dashboard/social-accounts` page), team members/users, media/content-gen library, and the entire admin dashboard (stats/users/tenants/plans/settings) — even though the backend already has real endpoints for campaigns (`GetCampaigns`, the `CreateCampaignStep1..7` flow) and admin (`GetPlatformStats`, `GetUsers`, `GetTenants`, `SetUserActive`). Wiring each of these up is the natural "next module."

## Known issues / things to fix, not forget

- **`server/Rawaj/Rawaj/appsettings.json` has an uncommitted local change**: the committed value is the safe placeholder `"REPLACE_WITH_CONNECTION_STRING"`, but the working copy on this machine currently has the real local SQL Server connection string in it (pre-existing before this session, not something to commit — real credentials shouldn't go into `appsettings.json` in git; they belong in `appsettings.Development.json` locally, ideally not even that if it stays tracked). **Don't `git add`/commit this file's connection string change.**
- No backend command exists to promote/demote a platform admin — only doable via direct SQL right now. Worth a real `SetPlatformAdmin` command if this needs to happen more than once.
- `preferredLanguage` is hardcoded to `'Ar'` at registration — no language picker in the sign-up UI yet.
- The backend's `ScheduledPostPublisherHostedService` background job fails repeatedly in the logs because `Encryption:Key` in `appsettings*.json` is still the placeholder `REPLACE_WITH_BASE64_ENCRYPTION_KEY` (not valid base64) — pre-existing, unrelated to this work, not fixed (out of scope).
- CORS is already fully open in `Program.cs` (`AllowAnyOrigin/AllowAnyMethod/AllowAnyHeader`) — no backend change needed there for local frontend↔backend calls.
- **LinkedIn OAuth is still a placeholder credential** in both `appsettings.json` and `appsettings.Development.json` (only Meta/Facebook has a real app configured) — the "ربط الحساب" button for LinkedIn on `/dashboard/social-accounts` will reach the redirect but fail on LinkedIn's side until a real app is registered.
- **No coin-spend hook exists yet.** Tenants are granted starting/reward coins, but nothing deducts them — the natural place is wherever AI content/campaign generation gets wired to real HTTP (content-gen is still mock), not this pass.
- The auth-module frontend changes were previously flagged as uncommitted directly on `dev` — since resolved (branched, committed, pushed as `feature/auth-module-real-backend-integration`); this session's tenant-lifecycle work should follow the same branch-and-push pattern.
- Items 12–15 (brand profiles → real HTTP, requirement gating, logo file storage, auth nav-link fixes) are **done in the working tree but not yet committed** — both `dotnet build` and `ng build` pass. Follow the same branch-and-push pattern before continuing.
- The brand-profile logo upload endpoint is standalone ("save file, return URL") and is **not** tied to any brand row, so abandoning the create wizard after uploading a logo leaves an orphan file under `wwwroot/media/brandprofiles/logourls/` — acceptable for now (matches how avatar uploads behave), but a cleanup pass could prune unreferenced logos later. The onboarding wizard still only records a logo *filename*, not a real uploaded file — wiring it to this same endpoint is a follow-up.

## Suggested next module

Brand profiles are now real (items 12–14), so **campaigns** are the natural
next module to wire to real HTTP — the backend already has `GetCampaigns` and
the `CreateCampaignStep1..7` flow, and campaigns hang off a brand profile
(`Campaign.brandProfileId`), which is now live. Wiring `CampaignService` from
its mock signal store to real `HttpClient` calls would also make the
brand-dependent gating in item 13 (campaigns/ads/calendar) operate on real
data end-to-end. Runner-up: the Settings "connections" tab (agency per-brand
social connections) is still mock and could be unified with the real
`/dashboard/social-accounts` OAuth flow.
