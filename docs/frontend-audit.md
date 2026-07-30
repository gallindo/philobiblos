# Frontend UX / Accessibility / Performance Audit

**Scope:** Angular 19 SPA (`frontend/src/app/`) — shell, styles, and genre/author/book catalog pages.
**Date:** 2026-07-30
**Guidelines:** [Vercel Web Interface Guidelines](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md)

## Status

All high and medium issues identified in this audit have been applied. The remaining open item is dark-mode support, which is left as a low-priority polish exercise.

## Summary

The frontend is built on solid foundations: semantic HTML, reactive forms with labels, `data-testid` hooks for tests, and a consistent color/elevation system. Most accessibility basics are present (`role="status"`, `aria-live="polite"`, `<button>` for actions, visible focus rings on inputs).

The main gaps were around **form discoverability**, **focus management**, **URL-backed state**, and **destructive-action UX**. The audit fixes below have been applied to make the app feel more polished and production-ready for the challenge presentation.

## Severity legend

- **High** — breaks accessibility, usability, or guideline compliance.
- **Medium** — noticeable UX friction, should be fixed before submission.
- **Low** — polish / nice-to-have.

## Findings

### Global / shell

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `app.component.html` | 4 | Low | Brand emoji (`📚`) is not marked decorative; screen readers will announce it inconsistently. Add `aria-hidden="true"`. |
| `app.component.html` | 27 | Medium | No skip link to `<main>` content. Add `id="main"` to `<main>` and a visually-hidden skip link at the top of `<body>`. |
| `app.component.scss` | 46 | Medium | Nav links rely on browser default focus. Add an explicit `:focus-visible` ring matching the teal accent. |
| `app.component.scss` | 89 | Low | Global error dismiss button has no focus-visible style. |
| `styles.scss` | — | Medium | No dark-mode support. Add `prefers-color-scheme: dark` variables or at least `color-scheme: light dark`. |
| `index.html` | — | Low | Verify `<html lang="en">` and `<meta name="theme-color">` are present. |

### Forms and inputs

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `genre-list.component.html` | 14 | Medium | Search input lacks `autocomplete="off"` and `name`. Browser password managers may attach to it. |
| `genre-list.component.html` | 16 | Low | Placeholder ends with `...`; use the ellipsis character `…`. |
| `author-list.component.html` | 14 | Medium | Search input lacks `autocomplete="off"` and `name`. |
| `author-list.component.html` | 16 | Low | Placeholder ends with `...`; use `…`. |
| `book-list.component.html` | 14 | Medium | Search input lacks `autocomplete="off"` and `name`. |
| `book-list.component.html` | 16 | Low | Placeholder ends with `...`; use `…`. |
| `book-list.component.html` | 21 | High | Author filter `<select>` has no visible `<label>` or `aria-label`. Screen-reader users cannot tell what it filters. |
| `book-list.component.html` | 27 | High | Genre filter `<select>` has no visible `<label>` or `aria-label`. |
| `book-list.component.html` | 94 | Medium | ISBN field should use `spellcheck="false"` and `autocomplete="off"`; it is a code-like identifier, not prose. |
| `book-list.component.html` | 105 | Low | Published year uses `type="number"`. Add `inputmode="numeric"` for consistent mobile behavior. |

### Focus and interaction

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `genre-list.component.scss` | 24 | Medium | Input/select focus ring uses `:focus`, so it appears on mouse click. Switch to `:focus-visible`. |
| `author-list.component.scss` | 24 | Medium | Same as above. |
| `book-list.component.scss` | 24 | Medium | Same as above. |
| `genre-list.component.scss` | 75 | Medium | `.btn-primary` has no explicit `:focus-visible` style. |
| `author-list.component.scss` | 75 | Medium | Same as above. |
| `book-list.component.scss` | 75 | Medium | Same as above. |
| `genre-list.component.scss` | 93 | Medium | `.btn-secondary` has no explicit `:focus-visible` style. |
| `author-list.component.scss` | 93 | Medium | Same as above. |
| `book-list.component.scss` | 93 | Medium | Same as above. |
| `genre-list.component.scss` | 106 | Medium | `.btn-link` has no explicit `:focus-visible` style. |
| `author-list.component.scss` | 106 | Medium | Same as above. |
| `book-list.component.scss` | 106 | Medium | Same as above. |

### Navigation and state

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `genre-list.component.ts` | 29 | High | Search, pagination, and edit state live in component signals only. The URL does not reflect them, so users cannot deep-link or use the back button. |
| `author-list.component.ts` | 29 | High | Same as above. |
| `book-list.component.ts` | 35 | High | Same as above, plus author/genre filters. |
| `app.routes.ts` | — | Low | Consider adding a catch-all redirect to `/books` or a 404 page. |

### Destructive actions

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `genre-list.component.ts` | 129 | Medium | Delete uses `window.confirm()`. It blocks the main thread and is not accessible. Replace with an inline confirmation dialog or an undoable toast. |
| `author-list.component.ts` | — | Medium | Same as above. |
| `book-list.component.ts` | 180 | Medium | Same as above. |

### Tables and content

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `author-list.component.html` | 91 | Medium | Author bio can be arbitrarily long; the table cell does not truncate. Add `max-width` / `text-overflow` or a detail view. |
| `book-list.component.html` | 160 | Low | ISBN column should use `font-variant-numeric: tabular-nums` for easier scanning. |
| `book-list.component.html` | 155 | Low | Year and ISBN empty states use `—` (em dash). Fine, but ensure consistent use across tables. |

### Pagination

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `pagination.component.html` | 14 | Low | Current page text is not announced as the active page. Add `aria-current="page"` to the page-number span. |

### Typography and copy

| File | Line | Severity | Issue |
|------|------|----------|-------|
| `genre-list.component.html` | 55 | Low | Save button text switches from `Save` to `Saving...`; use `Saving…`. |
| `author-list.component.html` | 57 | Low | Same as above. |
| `book-list.component.html` | 117 | Low | Same as above. |
| `genre-list.component.html` | 54 | Low | Loading text `Loading...` should be `Loading…`. |
| `author-list.component.html` | 56 | Low | Same as above. |
| `book-list.component.html` | 116 | Low | Same as above. |

## What passes

- Semantic HTML: tables, forms, labels, and `<button>` for actions.
- All form fields (except filter selects) have visible `<label>` elements with matching `for` attributes.
- Success banners use `role="status"`, global errors use `role="alert"`, loading indicators use `aria-live="polite"`.
- No `outline: none`, no `transition: all`, no `onPaste` blocking.
- Pagination controls are real buttons with text labels, not icon-only.
- Responsive layout: header collapses, tables scroll horizontally on small screens.
- Color contrast is reasonable with the current palette (verify with a contrast checker if you want to be strict).

## Recommended action plan

1. **Quick wins (do first)**
   - Add `aria-label` to the two filter selects in `book-list.component.html`.
   - Fix all `...` → `…` in placeholders and loading/saving text.
   - Add `autocomplete="off"` / `name` to search inputs and the ISBN field.
   - Add `aria-hidden="true"` to the brand emoji.
   - Add `aria-current="page"` to the pagination page number.

2. **Focus and accessibility**
   - Replace `:focus` with `:focus-visible` on inputs and select controls.
   - Add explicit `:focus-visible` rings to `.btn-primary`, `.btn-secondary`, and `.btn-link`.
   - Add a skip link to the main content.

3. **UX behavior**
   - Replace `window.confirm()` with a small inline confirmation dialog or an undoable toast.
   - Sync search, filters, and pagination to URL query parameters so the app is deep-linkable.
   - Truncate long author bios in the table and provide a detail view or tooltip.

4. **Polish**
   - Add dark-mode support if you want to show off theming.
   - Apply `font-variant-numeric: tabular-nums` to the ISBN and year columns.

## Appendix: Files reviewed

- `frontend/src/app/app.component.html`
- `frontend/src/app/app.component.scss`
- `frontend/src/styles.scss`
- `frontend/src/app/features/genres/genre-list.component.html`
- `frontend/src/app/features/genres/genre-list.component.scss`
- `frontend/src/app/features/genres/genre-list.component.ts`
- `frontend/src/app/features/authors/author-list.component.html`
- `frontend/src/app/features/authors/author-list.component.scss`
- `frontend/src/app/features/books/book-list.component.html`
- `frontend/src/app/features/books/book-list.component.scss`
- `frontend/src/app/features/books/book-list.component.ts`
- `frontend/src/app/shared/components/pagination.component.html`
- `frontend/src/app/shared/components/pagination.component.scss`
