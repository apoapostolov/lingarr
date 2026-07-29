# Proposal: Settings information architecture and interaction model

**Status:** Phase 1 implemented; Phases 2–4 remain proposed

**Scope:** Bedroom fork settings navigation, organization, validation, and a small set of operator controls

**Related:** [ai-providers-model-fallback-chain.md](../ai-providers-model-fallback-chain.md), [architecture.md](../architecture.md), [bedroom-reliability-development-plan.md](../bedroom-reliability-development-plan.md)

**Audience:** Product, UI, and backend contributors to Lingarr Next settings

---

## 1. Decision summary

Reorganize the existing settings without replacing Lingarr Next's interaction model.

1. Reduce the settings rail to five stable destinations: **Connections**, **Translation**, **Automation**, **System**, and **Plugins**.
2. Keep Lingarr Next's existing responsive card grid, compact navigation, theme tokens, and immediate save behavior.
3. Use route-backed tabs only where a destination contains distinct tasks. Do not add an in-page table of contents, accordion hierarchy, or a second permanent sidebar.
4. Add explicit **saving**, **saved**, **validation error**, and **save failed** feedback instead of a sticky Save bar and unsaved-change guard.
5. Promote Path mapping into Connections, but preserve its specialized editor and explicit save action.
6. Treat operational actions and status displays as purpose-built UI, not as stored setting field types.
7. Limit the first implementation to existing behavior plus the already-backed automation cursors. Evaluate search, presets, import/export, and broad plugin-schema changes separately.

This is an information-architecture correction, not a visual redesign or a vehicle for every possible operator preference.

---

## 2. Product brief

| Item | Decision |
|------|----------|
| **User** | A self-hosting operator who understands their media stack but should not need to know Lingarr Next's internal setting keys. |
| **Job** | Find and safely change translation, connection, automation, or system behavior without hunting through unrelated pages. |
| **Current behavior** | Nine flat settings links mix configuration pages with operational workspaces. Path mapping is routed but absent from the rail. Most fields save immediately, while Path mapping uses an explicit save action. |
| **Desired outcome** | Each setting has one predictable home; existing save semantics remain honest and visible; advanced tasks stay reachable without crowding the common path. |
| **Success signal** | A returning operator can predict where a setting lives, reach Path mapping from the UI, and tell whether a change is saved or failed. |
| **Non-goals** | Restyling Lingarr Next, changing Movies/Shows/Translations, exposing every environment variable, or building a universal administration console. |
| **Objects** | Settings, provider-chain rows, path mappings, automation cursors, and operational job/log views. |
| **Actions and consequence** | Most valid field changes persist immediately. Path mapping changes persist only when saved. Resetting a cursor changes where automation resumes; destructive history actions, if later approved, permanently remove records. |
| **Permissions** | Existing Lingarr Next authentication rules apply. No new role model is introduced. |
| **Open decisions** | Whether System uses four route-backed tabs or keeps Tasks and Logs as directly addressable child views; whether later usage evidence justifies cross-settings search. |

---

## 3. Current product language

The implementation establishes a recognizable Lingarr Next settings language:

- A compact left settings rail: icons at narrow widths, icon plus label from `md` upward.
- Route-level pages composed from a responsive grid of `CardComponent` surfaces.
- Cards use the product's `primary` / `secondary` / `tertiary` / `accent` theme tokens, rounded corners, restrained gradients, and short title/description pairs.
- Inputs, selects, toggles, and buttons come from shared components.
- Most valid field changes are debounced and persisted immediately through the setting store.
- Successful writes produce a small card-local **saved** notification.
- Dense tools such as Logs, Tasks, Path mapping, and Request templates use purpose-built full-width layouts instead of being forced into generic cards.
- Theme selection is global chrome in the application header, not a settings-page task.

The proposal must extend these patterns. A new page shell should not introduce a visually unrelated dashboard, an always-visible second navigation column, or a conventional form-submit model.

### Known inconsistencies worth fixing

- Path mapping is a valid route but is missing from the settings rail.
- Save feedback only represents success; a write in progress or a failed write is not visible.
- Some settings pages contain mixed tasks without a clear local hierarchy.
- Copy alternates between implementation terms (“Services”, “Indexer”) and user tasks (“Translation”, “Automation”).
- Logs and Path mapping use one-off control styling in places where shared buttons and focus treatment should be reused.

---

## 4. Proposed information architecture

### 4.1 Settings rail

```text
Settings
├── Connections
├── Translation
├── Automation
├── System
└── Plugins
```

Do not number the labels. The rail is navigation, not a setup wizard.

**Default landing:** always **Connections**, preserving the current stable Settings entry point. Do not change the landing page according to configuration state; conditional navigation would make bookmarks, the back button, and user expectations less predictable.

### 4.2 Local navigation

Use the existing tab language for distinct tasks inside a destination. Tabs must be route-backed so refresh, bookmarks, browser history, and direct links preserve the selected task.

At narrow widths, tabs may horizontally scroll or use a compact labeled control, but they must not become icon-only.

```text
Connections
├── Media servers
└── Path mapping

Translation
├── Setup
├── Subtitles
└── Advanced

System
├── Access
├── Tasks
└── Logs
```

Automation and Plugins remain single-page destinations until their content demonstrably needs subdivision.

### 4.3 Page shell

Keep the current responsive content model:

```text
┌──────────────────────────────────────────────────────┐
│ Optional route-backed tab bar                        │
├──────────────────────────────────────────────────────┤
│ Responsive card grid or a purpose-built workspace    │
│                                                      │
│ [Card] [Card] [Card]                                 │
└──────────────────────────────────────────────────────┘
```

Rules:

- Preserve the existing card spacing, radii, typography, and theme tokens.
- Use hierarchy, spacing, and alignment before adding nested cards.
- A page may use a full-width specialist workspace when the task is tabular or operational.
- Do not add an in-page table of contents to these short pages.
- Do not add Required / Recommended / Advanced accordions as a universal pattern. Reveal dependent fields inline and place genuinely specialist work under **Advanced**.
- Keep one emphasized action per card or specialist workspace.

---

## 5. Destination contents

### 5.1 Connections

#### Media servers

| Card | Contents |
|------|----------|
| **Radarr** | Address, API key, include new imports by default, Test connection, last successful check |
| **Sonarr** | Address, API key, include new imports by default, Test connection, last successful check |
| **Webhooks** | Existing webhook instructions and copyable endpoint |

Use the product names **Radarr** and **Sonarr** as the headings; avoid a generic “Integrations” card containing two unlabeled subsections.

Connection tests are explicit actions and do not change stored values. A test result appears within the relevant card and states what was tested.

#### Path mapping

Promote the existing mapping workspace into this destination without converting it to a generic setting card.

- Keep Source, Destination, and Media type visible.
- Keep Add mapping and Save mappings as named actions.
- Mark the workspace dirty when rows change.
- Disable Save mappings until every row is complete and valid.
- On save failure, preserve every edited row and show a recovery action.
- Removing an unsaved row is immediate. Removing an already-saved row takes effect only when Save mappings succeeds.

### 5.2 Translation

#### Setup

| Card | Contents |
|------|----------|
| **Translation services** | Existing ordered provider + model + API-key rows |
| **Languages** | Existing source and target language selectors |

The provider chain remains the primary workhorse:

- Each row stays self-contained.
- Row 1 is **Primary**; later rows are **Fallback 1**, **Fallback 2**, and so on.
- Provider, model, and credential fields stay in that order.
- Reordering or removing a fallback saves immediately only after the resulting chain is valid.
- The primary row cannot be removed.
- Missing credentials are explained on the affected row. Do not warn about credential-free providers.

#### Subtitles

| Card | Contents |
|------|----------|
| **Output** | Subtitle tag, language tag, translator information, captions |
| **Formatting** | Overlap fix, strip formatting, preserve line breaks |
| **Validation** | Existing validation switch and thresholds |

Use “Subtitles” for the destination task and short noun headings for cards. Avoid mixing “Quality”, “Output”, and “Validation” into one large card.

#### Advanced

| Card | Contents |
|------|----------|
| **Translation requests** | Batch mode, batch size, timeout, retries, retry delay, multiplier |
| **AI prompts** | System and context prompt controls |
| **Request templates** | Link to the existing provider-specific template workspace |

Dependent fields reveal inline. For example, Max batch size is visible only while batch translation is enabled. Provider-specific prompt and template controls appear only when at least one configured provider supports them.

### 5.3 Automation

At two-column widths, **Indexing** is the left card and **Automation** is the right card.

| Card | Contents |
|------|----------|
| **Library sync** | Movie and TV-show indexing schedules |
| **Automated translation** | Master switch, translation schedule, maximum translations per run |
| **File age** | Movie and TV-show age thresholds with units in the labels |
| **Cycle progress** | Read-only movie/show cursor status and reset actions |

Design requirements:

- The master switch applies immediately.
- Enabling automation requires valid translation services and a positive per-run limit. If not valid, keep the switch off and link to the field that needs attention.
- Schedules use a readable preset when possible, with raw cron available as an advanced input and a next-run preview.
- Cursor values are status, not editable number fields.
- Actions are named **Reset movie cycle** and **Reset TV-show cycle**.
- Before reset, explain that the next automation run starts scanning that media type from the beginning. The media library and translation history are not deleted.
- Reset actions show pending, success, and failure states and cannot double-submit.

### 5.4 System

System groups administration and diagnostics, but it does not flatten operational workspaces into cards.

| Tab | Contents |
|-----|----------|
| **Access** | Existing authentication toggle, API key, and user management |
| **Tasks** | Existing recurring-jobs workspace |
| **Logs** | Existing streaming log viewer |

The app version and development badge remain in global navigation. The theme picker remains in the global header.

A future **Maintenance** tab may be added only when it has at least one implemented status and one safe action. Do not create an empty destination around speculative Hangfire or SQLite controls.

### 5.5 Plugins

Keep Plugins as its own destination and preserve the current manifest-driven forms.

Core settings and plugin settings may reuse visual components, but they should not be forced into one schema in the first IA change. Plugin manifests are a public extension contract; expanding them requires compatibility and validation work of their own.

---

## 6. Save, validation, and feedback

### 6.1 Immediate-save fields

Most Lingarr Next setting fields continue to save after a valid change. The shared feedback state is:

```text
idle → editing → saving → saved
                     ↘ save failed → retry
editing + invalid → inline validation error (not persisted)
```

Requirements:

- Preserve the entered value through validation and recoverable failures.
- Debounced text fields must not report **saved** before the request succeeds.
- A failed request must state that the setting was not saved and offer Retry.
- Saving and saved feedback belongs to the affected card or field, not a global sticky bar.
- Route changes do not require an unsaved-change guard when all valid changes have already persisted.
- If a write is still in flight during navigation, let it finish and surface a failure through persistent notification or restored field state.

### 6.2 Explicit-save editors

Keep explicit save for compound objects where partial persistence would be invalid:

- Path mappings
- A future multi-setting preset review
- Settings import, if separately approved

These surfaces may use a sticky local action row on small screens only when the Save action would otherwise scroll out of reach.

### 6.3 Destructive and operational actions

Do not model actions as setting fields.

| Action impact | Confirmation |
|---------------|--------------|
| Reset automation cursor | Plain confirmation that names the media type and consequence |
| Clear only the current in-memory log view | No confirmation; the source log stream is not deleted |
| Permanently delete translation history | Dedicated future flow with object count, retention consequence, and explicit **Delete translation history** action |

Typed confirmation is reserved for high-impact irreversible actions. A checkbox and typed phrase are not both required by default.

---

## 7. Field vocabulary

### 7.1 First-wave reusable field types

Use a small vocabulary backed by real settings:

| Type | Control | Examples |
|------|---------|----------|
| `text` | Existing text input | Subtitle tag |
| `secret` | Existing password input | API keys |
| `url` | Text input plus separate Test action | Radarr/Sonarr address |
| `boolean` | Existing toggle | Automation enabled |
| `enum` | Existing select | Provider or media type |
| `integer` | Number input with min/max/step | Per-run limit, retries |
| `duration` | Number plus a fixed, visible unit | Timeout, retry delay, file age |
| `schedule` | Preset plus advanced cron input and next-run preview | Indexing and translation schedules |

Keep these as specialist components:

- `ordered_chain`
- `path_map`
- multi-language selection
- structured request templates

Keep these outside the setting-field vocabulary:

- Actions
- Status displays
- Presets
- Import/export

Do not add slider, percent, tag-list, arbitrary JSON, or preset-reference types until an approved setting needs them.

### 7.2 Optional core field metadata

If repeated form code justifies a catalog after the IA settles, use a client-owned core catalog first:

```ts
type CoreSettingField = {
  key: string
  type: 'text' | 'secret' | 'url' | 'boolean' | 'enum' | 'integer' | 'duration' | 'schedule'
  label: string
  description?: string
  section: string
  order?: number
  default?: string
  validation?: {
    required?: boolean
    min?: number
    max?: number
    step?: number
    pattern?: string
  }
  visibleWhen?: { key: string; equals: string }
  applies?: 'immediately' | 'next-job' | 'restart'
  searchTerms?: string[]
}
```

The catalog must use the same shared components as hand-written cards. It is an implementation aid, not a second visual system.

Do not add `GET /api/setting/catalog` in the first phase. Server-driven layout would couple the API to presentation and expand the plugin contract before the core interaction has been proven.

---

## 8. First approved additions

This proposal approves only additions that are already backed by current product behavior or a verified hard-coded policy.

| Addition | Surface | Behavior |
|----------|---------|----------|
| Test Radarr connection | Connections / Radarr | Read-only connection check; no setting key |
| Test Sonarr connection | Connections / Sonarr | Read-only connection check; no setting key |
| Movie automation cursor | Automation / Cycle progress | Read-only status plus **Reset movie cycle** |
| TV-show automation cursor | Automation / Cycle progress | Read-only status plus **Reset TV-show cycle** |
| `translation_history_retention_days` | Future System / Maintenance | Replaces CleanupJob's hard-coded seven-day retention after backend and migration work |

The cursor keys already exist:

- `automation_movie_processing_index`
- `automation_show_processing_index`

They remain internal state. The UI reads them as status and resets them through a named action endpoint; it does not write arbitrary index values.

### Deferred proposals

The following require product or backend behavior beyond a settings-screen reorganization and are not approved here:

- Automation dry run
- Subtitle scan modes or configurable exclusion directories
- Translation-quality heuristics and empty-line fallback rules
- Runtime log-level controls
- Editable Hangfire WAL checkpoint interval or vacuum schedule
- Plex, Jellyfin, or Bazarr refresh/notification hooks
- UI density, toast duration, sounds, or development-badge preferences
- Cross-page presets
- Settings import/export

Prefer a safe default or automatic behavior over adding a permanent control. Each deferred item needs its own object, consequence, default, failure states, and implementation evidence.

---

## 9. Search and presets

### 9.1 Search

Do not make settings search a Phase 1 requirement. First fix the naming, orphan route, and task grouping, then measure whether operators still fail to find controls.

If later justified, search should return navigable results grouped by destination. Selecting a result routes to the correct tab and focuses the field; it should not hide unrelated fields in place and leave the user without context.

Search terms may include user-facing synonyms. Raw setting keys may be searchable for operators, but they should not be displayed as primary labels.

### 9.2 Presets

Cross-page presets are deferred until the provider chain and automation safety rules are stable.

Any future preset flow must:

- Show a before/after diff grouped by destination.
- Never include or erase secrets.
- Apply all changes atomically or restore the previous values.
- State whether automation will be enabled and what the next run will do.
- Use a single review surface; do not stack confirmation modals.
- Save a custom configuration as custom rather than continually claiming a preset remains active.

Presets are actions over multiple settings, not a stored field type.

---

## 10. Responsive and accessibility requirements

- At narrow widths, the settings rail remains icon-only only when every item has an accessible name and tooltip; local tabs remain text-labeled.
- All primary tasks must be keyboard-completable with the shared visible focus treatment.
- Icon-only reorder, remove, refresh, add, and save controls require accessible names.
- Provider-chain rows stack vertically without horizontal scrolling.
- Long model names, paths, error messages, and localized labels must wrap or truncate with an accessible full value.
- Loading, empty, partial, validation, save-failed, and disabled states must be designed for every new data-backed surface.
- A disabled control must explain why the action is unavailable.
- Focus moves to inline validation summaries only when the error is not already adjacent to the field.

Keyboard shortcuts such as `/` for search or `S` for save are not part of the first implementation. Lingarr Next currently has no settings-shortcut vocabulary, and most settings do not use an explicit Save action.

---

## 11. Technical approach

### 11.1 Frontend

- Update `SettingPage.vue` to the five stable destinations.
- Add route-backed child tabs using the established `TabComponent` language.
- Recompose existing settings components before rewriting them.
- Keep `CardComponent`, shared fields, theme tokens, and responsive page grids.
- Extend the current saved notification into a small save-status component with `saving`, `saved`, and `failed` states.
- Keep the provider chain, Path mapping, Logs, Tasks, and Request templates specialized.
- Pilot any core field catalog on one simple page before migrating Integration or Automation wholesale.

### 11.2 Backend

- Keep the existing key/value and encrypted-setting storage.
- Add connection-test endpoints that return a bounded, user-facing result.
- Add dedicated cursor-status and cursor-reset endpoints; do not expose arbitrary cursor writes.
- Seed every new stored key through a migration. `SetSetting` must not be assumed to create a missing row.
- Add retention configuration only alongside the CleanupJob behavior and migration that consume it.

### 11.3 Compatibility

- Preserve old route paths as redirects:
  - `/settings/integration` → `/settings/connections/media-servers`
  - `/settings/mapping` → `/settings/connections/path-mapping`
  - `/settings/services` → `/settings/translation/setup`
  - `/settings/subtitle` → `/settings/translation/subtitles`
  - `/settings/authentication` → `/settings/system/access`
  - `/settings/tasks` → `/settings/system/tasks`
  - `/settings/logs` → `/settings/system/logs`
- Preserve provider-specific Request template deep links.
- Keep `service_type` rich JSON unchanged.
- Keep global environment bootstrap behavior unchanged.

---

## 12. Implementation phases

### Phase 0 — Interaction lock (complete)

- Confirm the five destination labels and route map.
- Capture current wide and narrow screenshots.
- Confirm immediate-save versus explicit-save behavior for every existing surface.

### Phase 1 — IA using existing components (implemented 2026-07-28)

- Update settings rail and redirects.
- Promote Path mapping under Connections.
- Recompose Translation and System with route-backed tabs.
- Preserve cards, specialist workspaces, theme picker, version badge, and auto-save.
- Add no new setting keys.

### Phase 2 — Honest save and validation states

- Add saving/saved/failed feedback.
- Preserve invalid or failed input.
- Add retry behavior for failed saves.
- Add connection tests.
- Improve compound Path mapping validation and dirty state.

### Phase 3 — Small operator additions

- Add cursor status and reset actions.
- Add retention only after the CleanupJob contract is specified and tested.
- Pilot `duration` and `schedule` controls where they replace existing raw-number or raw-cron inputs.

### Phase 4 — Evidence-based follow-up

- Measure findability and task completion.
- Consider cross-settings search only if the new IA remains insufficient.
- Write separate proposals for presets, import/export, runtime log configuration, or media-server refresh hooks.

Mobile and accessibility verification happen in every phase, not as a final polish phase.

---

## 13. Acceptance criteria

### Navigation

- Settings always opens Connections.
- Every former settings URL redirects to the equivalent new location.
- Path mapping is reachable from visible navigation.
- Refresh and browser history preserve the selected local tab.

### Interaction

- Valid simple changes persist without a page-level Save button.
- Save feedback appears only after the write result is known.
- Failed saves remain visible and retryable.
- Invalid input is retained locally and is not persisted.
- Path mapping retains its explicit Save mappings action and preserves edits on failure.

### Design language

- Existing theme tokens, card treatment, shared controls, and responsive grids remain in use.
- No permanent second sidebar, global sticky Save bar, universal accordion system, or settings-only keyboard vocabulary is introduced.
- Theme and version controls stay in global chrome.
- Logs, Tasks, Path mapping, and Request templates remain purpose-built workspaces.

### Safety

- Automation cannot be enabled with an invalid provider chain or non-positive per-run limit.
- Cursor resets name the media type and consequence.
- All new async actions have pending, success, and failure states and prevent double-submit.
- No secret is exposed by status, test, search, or future preset output.

---

## 14. Success measures

| Measure | Target |
|---------|--------|
| Find Path mapping from Settings | Under 10 seconds for a returning operator |
| Predict destination for provider, subtitle, automation, and log controls | At least 4 of 5 moderated attempts without hints |
| Save-state comprehension | Operator can distinguish saved, invalid, saving, and failed |
| Route compatibility | All former settings URLs land on an equivalent task |
| Narrow-screen task completion | Edit the provider chain and save a path mapping without horizontal page overflow |

Qualitative target: operators can describe the structure as “Connections, Translation, Automation, System, Plugins” without learning internal setting keys.

---

## 15. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Consolidation creates very long pages | Use route-backed tabs for distinct tasks; keep cards concise |
| Auto-save failures become more visible | Treat that visibility as correctness; provide retry and retain input |
| Route changes break bookmarks | Permanent redirects and route-level tests |
| Generic field rendering changes visual language | Pilot it on one page and require shared Lingarr Next components |
| System becomes a dumping ground | Admit only existing administration/diagnostic tasks; require a separate proposal for new operational behavior |
| New controls expose unsafe internals | Prefer read-only status plus named actions over editable raw values |

---

## 16. Open questions

1. Should System tabs live under one route with child paths, or remain separate child routes rendered through one tab shell? The visible behavior should be identical.
2. What exact state should Test connection report without leaking URLs, keys, or raw exceptions?
3. Should a cursor reset take effect immediately if an automation job is running, or be rejected until that job finishes?
4. What minimum and maximum retention values are safe for CleanupJob?
5. After Phase 1, does measured findability justify cross-settings search?

---

## 17. Recommendation

Approve **Phase 1** as the near-term product change. It fixes the orphan route and mental-model mismatch while preserving Lingarr Next's visual and interaction language.

Follow with **Phase 2** before adding broad new settings. Honest persistence and failure feedback are more valuable than a larger control catalog.

Approve **Phase 3** only for cursor status/reset and retention backed by implemented server behavior. Keep presets, import/export, runtime log controls, Hangfire tuning, media-server hooks, and appearance preferences out of this proposal until they have separate evidence and safety contracts.

---

## 18. Documentation maintenance

| When | Update |
|------|--------|
| Product behavior changes | This proposal and the affected active proposal in the same commit |
| Navigation changes | Router, `SettingPage.vue`, `docs/architecture.md`, and this route map |
| Setting key added | `SettingKeys.cs`, client types, migration seed, tests, and the relevant user-facing documentation |
| Provider-chain behavior changes | [ai-providers-model-fallback-chain.md](../ai-providers-model-fallback-chain.md) |

**Suggested branch:** `codex/settings-ia-qol`

**Deploy:** merge to the Bedroom fork, rebuild `lingarr-bedroom`, and never point Bedroom compose at official GHCR.
