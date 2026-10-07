# Shell and dashboard test IDs

These selectors are stable for Playwright. Prefer `getByTestId(...)`; use role locators for menu options that MudBlazor renders in popovers.

## App shell

| Test ID | Region / element | Expected interaction |
|---|---|---|
| `brand-mark` | Decorative circular LC monogram | Assert text is `LC`; the enclosing LifeCore Suite dashboard link navigates to `/dashboard`. |
| `global-search` | Top app bar search input | Fill and press `Enter`. `UW\d+` navigates to `/cases/{id}`; `LC\d+` navigates to `/policies/{id}`; any other text navigates to `/policies?query={text}`. |
| `persona-switcher` | Wrapper around MudSelect persona picker | Click the wrapper/input, then choose a listbox option by visible text such as `Dana Whitfield (Underwriter)`. |
| `env-badge` | Environment badge | Assert text is `DEMO`. |
| `nav-dashboard` | Left navigation Dashboard link | Click to navigate to `/dashboard`; active state is purple. |
| `nav-policies` | Left navigation Policies link | Click to navigate to `/policies`. |
| `nav-product-config` | Left navigation Product Config link | Click to navigate to `/products`. |
| `footer-disclaimer` | Footer disclaimer | Assert text is `Demo application. Synthetic data only.` |

## Dashboard KPI tiles

Each KPI tile has an outer test ID and an inner `[data-testid="kpi-value"]` for the numeric value.

| Test ID | Metric |
|---|---|
| `kpi-open-cases` | Open underwriting cases |
| `kpi-pending-requirements` | Cases pending requirements |
| `kpi-aps-pending` | APS pending |
| `kpi-avg-days` | Average days in underwriting |
| `kpi-sla-risk` | Cases at SLA risk |
| `kpi-decisions-today` | Decisions today |
| `dashboard-scope-all` | All cases toggle |
| `dashboard-scope-my` | My cases toggle, enabled for underwriter personas only. Selected by default for underwriters; switching persona re-applies the default. |
| `dashboard-scope-label` | Text describing the current scope, e.g. `Showing cases assigned to Dana Whitfield`. |

## Product configuration (`/products`)

| Test ID | Region / element | Expected interaction |
|---|---|---|
| `product-catalog` | Read-only product catalog table | Assert product names render. |
| `product-row` | Code cell of each product row | Also has `data-product-code` (`TERM`, `UL`, `IUL`, `FIA`, `VA`). |

## Worklist queue

| Test ID | Region / element | Expected interaction |
|---|---|---|
| `worklist-view-task` | Toggle item for task-grouped queue | Click to show task-type column. |
| `worklist-view-case` | Toggle item for case-grouped queue | Click to show open-task-count column. |
| `worklist-filter-status` | Wrapper around status MudSelect | Click, then select option by visible enum label (for example `Pending Requirements`). Clear with the MudBlazor clear icon when present. |
| `worklist-filter-product` | Wrapper around product MudSelect | Options are populated from loaded worklist rows; click and select by product name. |
| `worklist-filter-priority` | Wrapper around priority MudSelect | Click, then select `Rush`, `High`, `Normal`, or `Low`. |
| `worklist-search` | Worklist search text field | Fill applicant, case number, or product text; server data reloads after debounce. |
| `worklist-grid` | Worklist table container | Assert rows, headers, and empty/loading states. |
| `worklist-row` | First cell of each data row | Locator also has `data-case-number="UW..."`. Click the case-number button inside the cell, or click the table row, to open the summary. |
| `worklist-pager` | Pager region | Contains MudBlazor page-size controls plus LifeCore page info and next button. |
| `worklist-page-info` | Page info text | Assert current page and total item count. |
| `worklist-next-page` | Next-page button | Click to navigate to the next server page; disabled on the last page. |

## Case summary and chat

| Test ID | Region / element | Expected interaction |
|---|---|---|
| `case-summary-panel` | Right-side case summary drawer | Opens after selecting a worklist row. |
| `case-summary-close` | Close icon button | Click to close the drawer. |
| `case-summary-open-workbench` | Primary action button | Click to navigate to `/cases/{caseNumber}`. |
| `chat-toggle` | Peer chat collapse button | Click to load and show/hide case chat. |
| `chat-panel` | Chat panel region | Visible when expanded. |
| `chat-message` | Rendered chat message | One per message returned by the API. |
| `chat-input` | New chat message field | Fill message text. |
| `chat-send` | Send chat message button | Click posts through `LifeCoreApi.PostChatAsync` with current persona display name. |

## Charts

| Test ID | Chart |
|---|---|
| `chart-cases-by-status` | MudBlazor donut chart sourced from `DashboardKpisDto.CasesByStatus`. |
| `chart-weekly-intake` | MudBlazor bar chart sourced from `DashboardKpisDto.WeeklyIntake`. |
