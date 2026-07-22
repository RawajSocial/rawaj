# Frontend conventions

Practical, project-specific rules that build on `.claude/CLAUDE.md`. Read that
file first for general Angular/TypeScript style; this file covers patterns
specific to this codebase.

## Errors — always use ErrorModalService

Never build a one-off error dialog, alert box, or inline error banner.
Inject `ErrorModalService` (`src/app/services/error-modal.service.ts`) and
call `.show()`:

```ts
private readonly errorModalService = inject(ErrorModalService);

this.errorModalService.show('تعذّر إتمام العملية...', {
  variant: 'error',      // 'error' | 'warning' | 'info' | 'success'
  title: 'حدث خطأ غير متوقع', // optional — has a sensible default per variant
});
```

The actual dialog (`shared/components/error-modal`) is mounted once, globally,
in `app.html`, built on top of `ModalShell` — there is exactly one instance of
this markup in the whole app. Do not add another modal component for errors,
warnings, confirmations, or success toasts; extend the service/variant set
instead if a new visual state is needed.

## Loading states — use LoaderService, not a local spinner

For full-page/blocking loading (route transitions, long-running actions),
inject `LoaderService` (`src/app/services/loader.service.ts`) and call
`.show()` / `.hide()`. It's reference-counted, so overlapping calls are safe.
The page loader (`shared/components/page-loader`) is mounted once in
`app.html` and already wired to router navigation events in `app.ts`.

For a small in-place loading state scoped to one panel (e.g. content
generation output), it's fine to build a local animation — see
`content-gen-page`'s `.gen-orbit` for the pattern — but don't reintroduce a
second full-screen loader.

To preview the loader in isolation, visit `/dashboard/loading-test`.

## Styling — Bootstrap utilities + shared tokens, minimal custom CSS

- Prefer Bootstrap utility classes (`d-flex`, `gap-*`, `align-items-center`,
  the grid `row`/`col-*`, etc.) directly in templates over writing new CSS
  for layout. Only write CSS for things Bootstrap can't express (component
  visuals, animations, brand-specific styling).
- Use the CSS custom properties defined in `src/styles.css` (colors,
  spacing, radii, shadows, font sizes) instead of hardcoded values — e.g.
  `var(--color-danger)`, `var(--radius-lg)`, `var(--shadow-tag)`,
  `var(--font-size-sm)`. If you need a new shared value, add it as a
  variable in `styles.css` rather than repeating a magic number across files.
- `var(--shadow-tag)` (`0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)`)
  is the standard shadow for solid-color tags/badges, avatar circles, and
  circular icon buttons — use it instead of inventing a new shadow.
- Dashboard pages share `features/dashboard/dashboard-shared.css`
  (`.rw-card`, `.rw-btn`/`.rw-btn--dark`/`.rw-btn--outline`, `.rw-field`,
  `.rw-switch`, `.rw-pill`, `.rw-tabs`/`.rw-tab`). Import it via
  `styleUrls` and reuse these classes instead of rebuilding card/button/pill
  markup per page.
- Don't duplicate a whole component's HTML+CSS to reuse it elsewhere —
  extract a shared component (see `shared/components/`) the way
  `error-modal`, `modal-shell`, `file-upload`, `faq-section`, and
  `page-loader` were factored out.

## Directives — animations and tooltips

- **Tooltips**: use `appTooltip="النص هنا"` (`shared/directives/tooltip.directive.ts`)
  on any icon-only control (row actions, header icon buttons, etc.) instead
  of the native `title` attribute or a bespoke tooltip. Pair it with
  `aria-label` for accessibility — `data-tooltip` alone isn't read by screen
  readers. Optional `[position]` input: `'top' | 'bottom' | 'start' | 'end'`
  (defaults to `top`). It's pure CSS (`::after` + `attr(data-tooltip)`,
  styled in `styles.css`), so it needs no overlay/positioning service.
- **Scroll-reveal animations**: use `appGsapReveal="fade-up"` (or
  `fade-left`, `fade-left-far`, `fade-in`) with `[gsapDuration]`
  (`shared/directives/gsap-reveal.directive.ts`) for on-scroll entrance
  animations on landing-page sections. This replaced the old
  `RevealDirective`/`appReveal` (IntersectionObserver-based) — don't
  reintroduce that pattern.

## Shared building blocks (check before writing something new)

| Need | Use |
|---|---|
| Error/warning/info/success dialog | `ErrorModalService` + `error-modal` |
| Full-page loading overlay | `LoaderService` + `page-loader` |
| Any modal (confirm, form, custom content) | `shared/components/modal-shell` (`<app-modal-shell>` with `modal-header`/`modal-footer` projection slots) |
| Tooltip on an icon-only control | `TooltipDirective` (`appTooltip`) |
| Scroll-triggered entrance animation | `GsapRevealDirective` (`appGsapReveal`) |
| File/image upload with name + thumbnail preview | `shared/components/file-upload` |
| FAQ accordion | `shared/components/faq-section` |
| Page title + breadcrumbs | `shared/components/page-header` (wraps `shared/components/breadcrumb`) |

and seo services 


## Mock services + environment config

The app is fully mock-data-driven for now (no real HTTP calls). Services
like `CampaignService`, `AdService`, `BrandProfileService`, `TeamMemberService`,
and `MediaService` follow the same shape: a private writable `signal<T[]>`
with a public `.asReadonly()` getter, a `getById()` returning a `computed()`,
and plain mutation methods. Follow this pattern for new mock services rather
than introducing a different state-management style.

`src/environments/environment.ts` (+ `.development.ts` / `.production.ts`)
holds `apiUrl`, already pointed at the backend's `api/v1` routes
(`https://localhost:7206/api/v1` in dev) for whenever real HTTP integration
starts. The backend has no CORS policy configured yet — that needs to be
added server-side before the browser can actually call it.
