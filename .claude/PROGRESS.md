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
- `core/guards/` — `auth.guard.ts`, `admin.guard.ts`, `guest.guard.ts` (functional guards, `CanActivateFn`/`CanMatchFn`).
- `core/interceptors/` — `auth.interceptor.ts` (attaches bearer token, one silent refresh-and-retry on 401).
- `model/` — plain interfaces/types, one file per domain (`auth.model.ts`, `campaign.model.ts`, `brand-profile.model.ts`, `admin.model.ts`, `generated-item.model.ts`, `team-member.model.ts`, etc.). Auth model types are named to mirror the backend's C# records exactly (see below).
- `services/` — most are still **mock data stores**: `private signal<T[]>` + `.asReadonly()` + `getById()` computed + plain mutation methods (`CampaignService`, `AdService`, `BrandProfileService`, `TeamMemberService`, `MediaService`, `AdminService`). `AuthService` (in `core/auth/`, not `services/`) is the one exception — it's real, backed by the live API.
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

## What's real vs. still mock

- **Real (hits the live API)**: register, login, refresh-token, logout.
- **Everything else is still mock-data-driven** (local signals, no HTTP calls): campaigns, ads, brand profiles, team members/users, media/content-gen library, and the entire admin dashboard (stats/users/tenants/plans/settings) — even though the backend already has real endpoints for brand profiles (`GetBrandProfiles`, `GetBrandProfileById`, `CreateBrandProfile`, `UpdateBrandProfile`, `ArchiveBrandProfile`, `UploadBrandImage`, `RemoveBrandImage`), campaigns (`GetCampaigns`, the `CreateCampaignStep1..7` flow), and admin (`GetPlatformStats`, `GetUsers`, `GetTenants`, `SetUserActive`). Wiring each of these up is the natural "next module" after auth.

## Known issues / things to fix, not forget

- **`server/Rawaj/Rawaj/appsettings.json` has an uncommitted local change**: the committed value is the safe placeholder `"REPLACE_WITH_CONNECTION_STRING"`, but the working copy on this machine currently has the real local SQL Server connection string in it (pre-existing before this session, not something to commit — real credentials shouldn't go into `appsettings.json` in git; they belong in `appsettings.Development.json` locally, ideally not even that if it stays tracked). **Don't `git add`/commit this file's connection string change.**
- No backend command exists to promote/demote a platform admin — only doable via direct SQL right now. Worth a real `SetPlatformAdmin` command if this needs to happen more than once.
- `preferredLanguage` is hardcoded to `'Ar'` at registration — no language picker in the sign-up UI yet.
- The backend's `ScheduledPostPublisherHostedService` background job fails repeatedly in the logs because `Encryption:Key` in `appsettings*.json` is still the placeholder `REPLACE_WITH_BASE64_ENCRYPTION_KEY` (not valid base64) — pre-existing, unrelated to auth, not fixed (out of scope of the auth module work).
- CORS is already fully open in `Program.cs` (`AllowAnyOrigin/AllowAnyMethod/AllowAnyHeader`) — no backend change needed there for local frontend↔backend calls.
- The auth-module frontend changes above are currently **uncommitted on `dev` directly** (not on a feature branch) — branch them off before pushing, per this repo's usual workflow (see git history for the `feature/*` branch naming pattern used throughout this project).

## Suggested next module

Since auth is done, the natural next step is wiring one real-data module end
to end (brand profiles is the most complete on the backend side — full CRUD +
image upload already exist) to replace its mock service with real
`HttpClient` calls through the now-working `AuthService`/interceptor, as a
template for doing the same to campaigns, ads, team members, and the admin
dashboard afterward.
