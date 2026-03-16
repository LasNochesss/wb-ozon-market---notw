# Premium Notebook Design System — Marketplace Sales Accounting App

## 1. Style & Mood
- Premium minimalism: luxury notebook / executive planner / high-end fintech dashboard.
- Soft neutral canvas with subtle paper-like feeling.
- Card-based layout with gentle shadows, precise borders, rounded corners (10–14px).
- Low visual noise, clear information hierarchy, long-session comfort.

## 2. Design Tokens

### 2.1 Color Tokens

#### Light (default)
- `color.bg.canvas`: `#F7F6F3`
- `color.bg.surface`: `#FFFFFF`
- `color.bg.surface.soft`: `#FCFCFA`
- `color.border.default`: `#E7E5DF`
- `color.border.strong`: `#D9D6CE`
- `color.text.primary`: `#161A22`
- `color.text.secondary`: `#6A7280`
- `color.text.tertiary`: `#9097A3`
- `color.accent.primary`: `#2F3E6E`
- `color.accent.primary.hover`: `#26355F`
- `color.accent.secondary`: `#6B7280`
- `color.state.success`: `#2E8B57`
- `color.state.success.bg`: `#EAF6EF`
- `color.state.warning`: `#B7791F`
- `color.state.warning.bg`: `#FFF6E8`
- `color.state.error`: `#C2413B`
- `color.state.error.bg`: `#FDEEEE`
- `color.focus.ring`: `#9AA8D6`

#### Dark (optional)
- `color.bg.canvas`: `#15181E`
- `color.bg.surface`: `#1D222B`
- `color.bg.surface.soft`: `#232A35`
- `color.border.default`: `#2F3744`
- `color.border.strong`: `#3A4454`
- `color.text.primary`: `#E8ECF3`
- `color.text.secondary`: `#A3ADBC`
- `color.text.tertiary`: `#7B8698`
- `color.accent.primary`: `#7E8FCB`

### 2.2 Typography Tokens
- UI font: `Segoe UI Variable` (fallback `Inter`).
- Numeric/accent font: `Bahnschrift`/`JetBrains Sans` (tabular numerals).
- `font.size.display`: `28`
- `font.size.h1`: `22`
- `font.size.h2`: `18`
- `font.size.table.header`: `13`
- `font.size.body`: `13`
- `font.size.caption`: `12`

### 2.3 Spacing, Radius, Shadows
- Spacing scale: `4, 8, 12, 16, 20, 24, 32`.
- Radius: `8 / 12 / 14`.
- Shadow card: `0 1 2 / 0 8 24` soft layered.
- Layout: 12-column desktop grid, wide margins, breathable spacing.

## 3. Component Guidelines

### Buttons
- Variants: Primary / Secondary / Ghost / Danger / Disabled / Loading.
- Primary = accent fill, 12px radius, semibold.

### Inputs
- Text input, search, date picker, marketplace selector.
- Clear focus ring and large hit area.

### Tables
- Sticky headers, zebra rows, hover/selected row.
- Numeric columns right aligned, tabular numerals.
- Sort + filter + inline edit states.

### Pills/Badges
- Status (`Активен`, `Неактивен`), sale type (`Реальная`, `Самовыкуп`).

### Cards/Sections
- Each business block as a premium card section (expand/collapse).

### Toast / Modal / Drawer / Empty States
- Non-intrusive toasts.
- Modals for critical confirmation.
- Drawer for settings/import mapping.
- Empty-state guidance with action buttons.

## 4. Interaction Spec

### Motion
- Standard: `180–220ms`, easing `easeOutCubic`.
- Expand/collapse: `220–260ms`.
- Press: `90ms`.
- Table hover: `120ms`.
- KPI count-up: `600–900ms`.
- Cell-change flash: `~700ms` fade-out.

## 5. Financial Readability Rules
- Revenue/price visually strongest (size + contrast).
- Profit/loss: color + arrow + subtle background.
- Margin `%`: badge/mini-bar style.
- Mini-summary in each section header.

## 6. Screen Prototypes

### 6.1 Main Screen
- Top bar + filters + actions + vertical business sections:
  - Товары
  - Курсы валют
  - Закупки
  - Логистика
  - Продажи
  - Маркетинг
  - Прочие расходы
  - Склад
  - Юнит-экономика
  - P&L + KPI

### 6.2 P&L + KPI Screen
- KPI cards, period filters, trend charts, structure charts, top SKU.

### 6.3 Project / Settings / Import Screen
- Project DB settings, backup, import/export, column mapping, validation report.

## 7. UX Features
- Hotkeys (`Ctrl+N`, `Ctrl+S`, `Ctrl+F`, `F2`, `Ctrl+R`).
- Persisted UI state (expanded sections, filters, column widths).
- Recalculation progress + change log.
- Human-friendly validation hints.

## 8. Implementation Notes (WPF)
- Keep tokens in `ResourceDictionary` files (Colors, Typography, Spacing, Shadows, Controls).
- Reuse styles globally from `App.xaml` merged dictionaries.
- Use `Storyboard` transitions for subtle interactions.
- Keep data model untouched; apply visual layer only.
