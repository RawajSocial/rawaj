# Rawaj — Marketing Automation Platform

**Rawaj** is an Angular 21 web application for a marketing agency that helps business owners and agencies create AI-powered ads and automate social media publishing. The UI is fully Arabic (RTL) with a dark cosmic theme.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | Angular 21.2 (Standalone API) |
| Language | TypeScript 5.9 |
| Styling | CSS Variables + Bootstrap 5.3 |
| Forms | Angular Reactive Forms |
| Routing | Angular Router (lazy-loaded) |
| Build | Angular CLI + Vite |
| Testing | Vitest |
| Fonts | Cairo · Noto Kufi Arabic (Google Fonts) |
| Icons | Font Awesome 7.2 |
| Language | Arabic (RTL) |

---

## Getting Started

```bash
npm install
npm start          # dev server → http://localhost:4200
npm run build      # production build
npm test           # run tests
```

---

## Project Structure

```
src/
├── styles.css                  # Design system (tokens, global base, Bootstrap overrides)
├── styles/
│   ├── custom.button.css       # CTA star-animation button
│   ├── auth.checkbox.css       # Animated checkbox (shared login/signup)
│   └── reveal.css              # Scroll reveal animation classes
│
└── app/
    ├── app.routes.ts           # All routes (lazy-loaded)
    ├── core/                   # Auth, guards, interceptors, SEO, themes (stubs)
    ├── services/               # FormErrors, SEO, RevealObserver services
    ├── model/                  # Shared TypeScript interfaces
    ├── shared/
    │   ├── components/         # StepBadge, StepHeading
    │   └── directives/         # RevealDirective (scroll animation)
    ├── layout/                 # Admin / user layout shells (stubs)
    ├── store/                  # State management (stub)
    ├── environments/           # Environment config (stub)
    └── features/
        ├── landing/            # Marketing homepage (9 sections)
        ├── auth/
        │   ├── login/          # Login page (3-component layout)
        │   └── sign-up/        # Sign-up page (7-field reactive form)
        ├── dashboard/          # User dashboard (stub)
        └── on-boarding/        # 6-step onboarding wizard
```

---

## Pages & Routes

| Route | Component | Status |
|---|---|---|
| `/` | Landing | Complete |
| `/login` | Login | Complete |
| `/sign-up` | Sign Up | Complete |
| `/on-boarding` | RawajOnboarding | Complete |
| `/dashboard` | Dashboard | Stub |

---

## Landing Page Sections

1. **Navbar** — logo, nav links, CTA button
2. **Hero** — headline, CTA buttons, dashboard preview, animated circuit grid
3. **WhyUs** — 6 feature cards + infinite brands marquee
4. **HowItWorks** — vertical timeline with cosmic background
5. **Gallery** — masonry grid of client ads (`@defer` lazy loaded)
6. **Posts** — RTL carousel of featured social media posts
7. **Pricing** — plan cards with featured glowing card
8. **CTA** — conversion section with cosmic gridlines
9. **Footer** — 3-column brand footer

---

## Onboarding Flow (6 Steps)

| Step | Content |
|---|---|
| 1 | Account type — Brand, Agency, E-commerce, Creator |
| 2 | Brand info — name, Instagram, website, sector, stage, location |
| 3 | Product & target audience |
| 4 | Goals & budget |
| 5 | Brand identity & visual references |
| 6 | AI strategy questionnaire |
| ✓ | Success / completion screen |

Progress is persisted to `localStorage`. The sidebar stepper and top progress bar update dynamically on each step.

---

## Design System

All design tokens live in [`src/styles.css`](src/styles.css) as CSS custom properties on `:root`.

### Color Palette

#### Raw Scales (50 = lightest, 600 = darkest)

| Scale | 50 | 300 | 600 |
|---|---|---|---|
| `--color-blue-gray-*` | `#DFE2EE` | `#5E6F9B` | `#0B0F19` |
| `--color-purple-*` | `#DBD3FB` | `#7C3AED` | `#170339` |
| `--color-blue-*` | `#D9DEFD` | `#2563EB` | `#020E30` |
| `--color-yellow-*` | `#FFEFD1` | `#9F8109` | `#291F01` |
| `--color-gray-*` | `#FFFFFF` | `#898989` | `#242424` |
| `--color-cool-gray-*` | `#E2E2E3` | `#707073` | `#0F0F10` |

#### Brand Tokens

```css
--color-brand-dark:   #0B0F19   /* page background */
--color-brand-purple: #7C3AED   /* secondary / accent */
--color-brand-blue:   #2563EB   /* primary / CTAs */
--color-brand-yellow: #FACC15   /* highlight / accent */
--color-brand-white:  #FFFFFF   /* text inverse */
```

#### Semantic Aliases — use these in components

```css
/* Interactive */
--color-primary           /* blue — main CTA buttons */
--color-primary-hover
--color-secondary         /* purple — accent buttons */
--color-accent            /* yellow — highlights */

/* Text */
--color-text-primary      /* main body text */
--color-text-secondary    /* muted labels */
--color-text-muted
--color-text-inverse      /* white text on dark bg */
--color-text-link

/* Surfaces */
--color-bg                /* white (#FFFFFF) */
--color-bg-secondary      /* card backgrounds (#F8F9FC) */
--color-surface

/* Borders */
--color-border            /* default input/card border */
--color-border-focus      /* focused input ring (blue) */

/* Status */
--color-success  --color-danger  --color-warning  --color-info
```

### Spacing Scale

`--space-1` (4px) · `--space-2` (8px) · `--space-3` (12px) · `--space-4` (16px) · `--space-6` (24px) · `--space-8` (32px) · `--space-12` (48px) · `--space-16` (64px) · `--space-20` (80px) · `--space-24` (96px)

### Border Radius

`--radius-xs` (2px) · `--radius-sm` (4px) · `--radius-md` (8px) · `--radius-lg` (12px) · `--radius-xl` (16px) · `--radius-2xl` (24px) · `--radius-full` (9999px)

### Transitions

```css
--transition-fast:   150ms ease
--transition-base:   250ms ease
--transition-slow:   400ms ease
--transition-bounce: 300ms cubic-bezier(0.34, 1.56, 0.64, 1)
```

### Z-Index Stack

```css
--z-below: -1  →  --z-raised: 10  →  --z-dropdown: 100  →  --z-sticky: 200
→  --z-overlay: 300  →  --z-modal: 400  →  --z-toast: 500  →  --z-tooltip: 600
```

### Typography

| Variable | Font | Usage |
|---|---|---|
| `--font-sans` | Cairo | Body, UI, labels, buttons |
| `--font-serif` | Noto Kufi Arabic | h1–h3, display headings |

`h1–h3` automatically use `--font-serif`. Utility classes: `.font-cairo` · `.font-kufi` · `.font-light` → `.font-black`

---

## Shared Services

### SeoService — `src/app/services/seo.service.ts`

Sets title, meta description, keywords, canonical URL, Open Graph, and Twitter Card tags per page.

```typescript
constructor(private seo: SeoService) {}

ngOnInit() {
  this.seo.setPageSeo({
    title: 'عنوان الصفحة',
    description: 'وصف الصفحة',
    keywords: 'كلمة, كلمة',
    path: '/route',
    noIndex: false,   // true for auth/private pages
  });
}
```

Currently applied to: `/` · `/login` · `/sign-up` · `/dashboard` · `/on-boarding`

### FormErrorsService — `src/app/services/form-errors.service.ts`

Returns Arabic validation messages for reactive form controls.  
Supports: `required` · `email` · `minlength` · `requiredTrue` · password mismatch.

### RevealDirective — `src/app/shared/directives/reveal.directive.ts`

Single shared `IntersectionObserver` for scroll-reveal animations. SSR-safe, respects `prefers-reduced-motion`.

```html
<!-- Import RevealDirective in the component's imports array first -->
<section appReveal="fade-up" [revealDuration]="600" [revealDelay]="100">
  ...
</section>
```

**Variants:** `fade-up` · `fade-down` · `fade-left` · `fade-right` · `fade-up-far` · `fade-down-far` · `fade-in` · `fade-out`

---

## Adding a New Page

1. Create `src/app/features/<name>/<name>.ts` as a standalone component
2. Add a lazy route in [`src/app/app.routes.ts`](src/app/app.routes.ts):
   ```typescript
   {
     path: 'page-name',
     loadComponent: () =>
       import('./features/page-name/page-name').then(m => m.PageNameComponent)
   }
   ```
3. Inject `SeoService` and call `setPageSeo()` in `ngOnInit`
4. Add `appReveal` to sections for scroll-in animations

---

## Known Stubs (Next Steps)

| Area | Location | What's Needed |
|---|---|---|
| Auth guard | `src/app/core/guards/` | Protect `/dashboard` and `/on-boarding` |
| HTTP interceptor | `src/app/core/interceptors/` | Attach JWT to API requests |
| Auth service | `src/app/core/auth/` | Login/signup API calls, token storage |
| State management | `src/app/store/` | NgRx or signals-based store for user/session |
| Environments | `src/app/environments/` | API base URL per build environment |
| Dashboard | `src/app/features/dashboard/` | Full user dashboard UI |
| Backend sync | — | Persist onboarding data to API (currently localStorage only) |
| Layout shells | `src/app/layout/` | Admin and user layout wrappers with sidebar nav |
