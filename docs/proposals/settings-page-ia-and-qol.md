# Proposal: Settings page IA, organization, and quality-of-life setting types

**Status:** Draft proposal (not implemented)  
**Scope:** Bedroom fork settings UX + setting model extensions  
**Related:** [ai-providers-model-fallback-chain.md](../ai-providers-model-fallback-chain.md), [architecture.md](../architecture.md), [bedroom-reliability-development-plan.md](../bedroom-reliability-development-plan.md)  
**Audience:** Product / UI / backend for Lingarr settings

---

## 1. Problem statement

Settings has grown by accretion. Each new capability added another top-level nav item or a card on an existing page. After the AI provider + fallback chain work, **Services** is dense and power-user oriented, while other pages remain flat lists of toggles with little hierarchy, search, or progressive disclosure.

Operators (Bedroom media stack) need to:

1. Find the right control in seconds (not by hunting nine sidebar items).
2. Understand **what changes with a save** (integrations vs translation quality vs automation risk).
3. Express preferences that today live only as env vars, hard-coded defaults, or tribal knowledge (log noise, scan safety, Hangfire/SQLite care, post-translate refresh, retention).
4. Avoid dangerous misconfiguration (empty API keys on every fallback row, automation on a huge library with no age threshold, etc.).

This proposal reorganizes Settings **information architecture (IA)**, defines **new quality-of-life (QoL) setting types**, and outlines a phased implementation that stays compatible with the existing key/value store and plugin field model.

---

## 2. Current state (as of `main` / bedroom)

### 2.1 Navigation (flat sidebar)

| Route | Label | Content today |
|-------|--------|----------------|
| `integration-settings` | Integrations | Radarr / Sonarr URL, API keys, default include |
| `authentication-settings` | Authentication | Auth toggle, users, API key |
| `services-settings` | Services | Provider+model fallback chain, API keys; adjacent **Translation** card (prompts, batch, retries) |
| `subtitle-settings` | Subtitle | Output naming/tags + **Validation** card |
| `automation-settings` | Automation | Enable, schedules, max per run, age thresholds |
| `plugins-settings` | Plugins | Dynamic plugin forms |
| `tasks-settings` | Tasks | Hangfire recurring job UI |
| `logs-settings` | Logs | In-memory log viewer |
| `telemetry-settings` | Telemetry | Opt-in telemetry |
| *(orphan route)* `mapping-settings` | Path mapping | Exists in router, **not** in settings sidebar |
| *(child)* `request-template-settings` | Request templates | Deep link from Services, not in nav |

### 2.2 Storage model

- Flat `settings` table: `key` / `value` / optional `provider`.
- Keys grouped only in code (`SettingKeys.*`).
- Plugins declare fields via manifests; Services chain serializes rich JSON into `service_type`.
- Secrets encrypted for known API key keys.

### 2.3 Pain points

1. **IA mismatch with mental model** — “How do I translate?” spans Services + Subtitle + Automation + Mapping + Tasks.
2. **Mapping is orphaned** — critical for Docker path mounts, hidden from the settings nav.
3. **Services + Translation co-located** — chain editor and prompt/batch knobs compete for attention; first-time setup vs fine-tuning is not staged.
4. **No search / no “changed” indicator** — large installs cannot find `max_retries` or age thresholds quickly.
5. **No presets** — “safe bulk free translate” vs “quality AI only” require manual multi-page setup.
6. **Ops knobs missing from UI** — Hangfire WAL interval, automation cursor reset, log level, retention, media-server refresh after translate (skill-level ops only).
7. **Validation is late** — bad chain (all free scrapers + empty keys) surfaces at job failure, not at save.
8. **Mobile nav** — icon-only rail is easy to mis-tap; long pages lack in-page anchors.

---

## 3. Goals and non-goals

### Goals

| ID | Goal |
|----|------|
| G1 | Reorganize Settings into **task-oriented groups** with clear progressive disclosure. |
| G2 | Surface **orphan and ops settings** in the UI (mapping, maintenance, logging). |
| G3 | Introduce **typed QoL controls** (not only free-text / boolean) with validation and defaults. |
| G4 | Support **presets / profiles** for common operator modes without forking config files. |
| G5 | Keep the **key/value backend**; migrate via additive keys + optional JSON blobs. |
| G6 | Stay fork-safe: no dependency on upstream GHCR; Bedroom can ship ahead of upstream. |

### Non-goals (this proposal)

- Full redesign of Movies/Shows/Translations list UIs.
- Replacing Hangfire or SQLite with another stack (only expose safe knobs).
- Multi-tenant / multi-library org settings.
- Real-time collaborative settings editing.

---

## 4. Proposed information architecture

### 4.1 Top-level groups (sidebar)

Collapse nine flat items into **five primary destinations** plus one advanced:

```text
Settings
├── 1. Connections      ← Sonarr, Radarr, path mapping, webhooks summary
├── 2. Translation      ← Chain, languages, prompts, output, validation
├── 3. Automation       ← Schedules, limits, age, include defaults, cursors
├── 4. Appearance & UX  ← Theme, toasts, navigate-on-request, density (new)
├── 5. System           ← Auth, tasks, logs, telemetry, maintenance (new)
└── Plugins             ← Keep separate (dynamic; third-party)
```

**Default landing:** `Connections` if Sonarr/Radarr incomplete; else `Translation`.

### 4.2 Page layout pattern (all settings pages)

```text
┌─────────────────────────────────────────────────────────────┐
│ Title · short description · [Search settings] · [Presets ▾] │
├──────────────┬──────────────────────────────────────────────┤
│ In-page TOC  │  Section cards (sticky save bar on change)   │
│ (anchors)    │  · Required / Recommended / Advanced folds   │
└──────────────┴──────────────────────────────────────────────┘
```

Shared chrome:

- **Sticky save bar** when dirty (page-scoped or global setting store dirty map).
- **Unsaved guard** on route leave.
- **Search** filters cards/fields by label + key + help text.
- **Reset section to defaults** (per card).
- **“Used by”** footer on fields that affect automation vs manual translate.

### 4.3 Substructure by page

#### Connections

| Card | Contents |
|------|----------|
| Sonarr | URL, API key, test connection, default include, last sync status |
| Radarr | Same |
| Path mapping | Host ↔ container path rules (promote Mapping into nav) |
| Webhooks | Read-only endpoints + copy; link to docs (from existing WebhookInstructions) |

#### Translation

| Card | Contents |
|------|----------|
| Languages | Source / target multi-select (existing SourceAndTarget) |
| Provider chain | Current ServicesSettings chain UI (primary workhorse) |
| Output & naming | Tags, remove language tag, translator info, captions |
| Quality & validation | Overlap fix, strip formatting, preserve breaks, validation thresholds |
| Reliability | Timeout, retries, delay, multiplier; batch size |
| Prompts & templates | AI prompt, context, deep link to request templates |

Use **tabs or accordion**: *Setup* (languages + chain) → *Output* → *Advanced*.

#### Automation

| Card | Contents |
|------|----------|
| Master switch | Enable automation + plain-language risk note |
| Throughput | Max per run, movie/show schedules |
| Freshness | Age thresholds (global + note on per-title overrides) |
| Cycle state | **Show/reset durable processing indices** (new; today settings keys only after reliability work) |
| Library defaults | Link to include-all tools; default include from *arr |

#### Appearance & UX (new)

| Card | Contents |
|------|----------|
| Theme | Existing theme picker (if any) surfaced here |
| Notifications | Toast duration, success/error sound off, density |
| Navigation | Navigate to details on request; open translations in new tab |
| Development chrome | Show/hide Development pill in non-dev builds (optional) |

#### System

| Card | Contents |
|------|----------|
| Authentication | Existing auth + users |
| Background jobs | Tasks/schedule UI embed or link |
| Logs | Viewer + **log level** + clear buffer |
| Maintenance | Clear translation history, Hangfire checkpoint status, WAL size hint, vacuum schedule |
| Telemetry | Existing |
| About | Version, image tag, docs links |

#### Plugins

Unchanged entry point; ensure plugin fields support new field types (below).

---

## 5. New quality-of-life setting **types**

Today UI fields are mostly: text, password, boolean, select, multi-language JSON. Propose a **typed field vocabulary** shared by core settings and plugins.

### 5.1 Control types

| Type | UI | Validation | Example uses |
|------|-----|------------|--------------|
| `boolean` | Toggle + optional danger confirm | — | Automation enabled |
| `string` / `secret` | Text / password | min/max length, pattern | API keys |
| `url` | Text + **Test** button | scheme/host reachability | Sonarr URL |
| `enum` | Select | allow-list | Log level |
| `multi_enum` | Multi-select chips | non-empty when required | Source languages |
| `int` / `float` | Number stepper | min/max/step | Max translations, temperature |
| `duration` | Value + unit (s/m/h) | range | Retry delay, request timeout, age threshold |
| `cron` / `schedule` | Preset chips + advanced cron | parse + next-run preview | Movie/show schedules |
| `path` | Text + browse modal | exists in container (optional) | Mapping paths |
| `path_map` | List editor (from→to) | no cycles, unique from | Path mappings |
| `ordered_chain` | Current provider+model rows | ≥1 row, unique constraints optional | `service_type` chain |
| `json` | Monaco/code or structured form | schema | Rare power-user |
| `preset_ref` | Card picker | — | Apply profile |
| `action` | Button (not stored) | confirm | “Reset automation cursor”, “Clear translations”, “Checkpoint Hangfire WAL” |
| `status` | Read-only badge/meter | — | Last sync, WAL size, queue depth |
| `percent` / `slider` | Slider | 0–100 | Toast opacity / confidence thresholds (future) |
| `tag_list` | Chip input | charset | Subtitle tags, ignored folder names |

### 5.2 Field metadata (schema extension)

Each field declaration (core catalog or plugin) should support:

```ts
type SettingField = {
  key: string
  type: FieldType
  label: string
  description?: string
  group: string          // card id
  order?: number
  importance: 'required' | 'recommended' | 'advanced'
  default?: string
  dependsOn?: { key: string; equals?: string; notEmpty?: boolean }
  danger?: boolean       // confirm + red affordance
  restartHint?: boolean  // “applies on next job / restart”
  tags?: string[]        // search: ['automation','performance']
}
```

**DependsOn** enables progressive disclosure (e.g. batch size only if batch enabled; model only if AI provider).

### 5.3 New **setting keys** (QoL product surface)

Proposed additive keys (names illustrative; final keys follow `SettingKeys` conventions):

#### Operator / library safety

| Key | Type | Purpose |
|-----|------|---------|
| `automation_dry_run` | boolean | Log what would translate without creating jobs |
| `automation_skip_if_target_exists` | boolean | Explicit; align with hash/skip behavior, make user-visible |
| `subtitle_exclude_dirs` | tag_list | Extra junk dirs beyond built-in Trailers/Featurettes |
| `subtitle_scan_mode` | enum | `adjacent` \| `subs_folder` \| `legacy_deep` (gate deep scan) |
| `library_size_warning_threshold` | int | Warn in UI when episode count &gt; N |

#### Translation QoL

| Key | Type | Purpose |
|-----|------|---------|
| `preferred_chain_profile` | enum | `free_bulk` \| `quality_ai` \| `custom` |
| `fallback_on_empty_translation` | boolean | Treat empty line as failure → next chain row |
| `min_translated_line_ratio` | percent | Fail job if too many lines empty/unchanged |
| `notify_on_job_complete` | boolean | Toast / future webhook |
| `default_manual_provider_index` | int | Which chain row “Translate” uses first |

#### Automation cursors & maintenance

| Key | Type | Purpose |
|-----|------|---------|
| `automation_movie_processing_index` | int + action reset | Already backend; **expose + Reset** |
| `automation_show_processing_index` | int + action reset | Same |
| `translation_history_retention_days` | int | Drive CleanupJob instead of hard-coded week |
| `hangfire_wal_checkpoint_minutes` | int | Surface env `HANGFIRE_WAL_CHECKPOINT_MINUTES` |
| `clear_translation_history` | action | Operator wipe (with confirm) |

#### Logging & diagnostics

| Key | Type | Purpose |
|-----|------|---------|
| `log_level_default` | enum | Information / Warning / Debug |
| `log_level_hangfire` | enum | Separate Hangfire noise |
| `log_level_sync` | enum | Quiet Sonarr/Radarr sync |
| `logs_buffer_size` | int | In-memory sink capacity |
| `logs_clear` | action | Clear buffer |

#### Media ecosystem (Bedroom-oriented)

| Key | Type | Purpose |
|-----|------|---------|
| `refresh_plex_after_translate` | boolean | Hook existing refresh skill/script |
| `refresh_jellyfin_after_translate` | boolean | Same |
| `bazarr_notify` | boolean | Optional bridge flag |

#### UX

| Key | Type | Purpose |
|-----|------|---------|
| `ui_density` | enum | comfortable / compact |
| `toast_duration_ms` | duration | |
| `settings_show_advanced_by_default` | boolean | |
| `settings_last_section` | string | Remember last settings route |

---

## 6. Presets / profiles

One-click packages that write multiple keys (and can be re-applied):

| Preset | Intent | Representative writes |
|--------|--------|------------------------|
| **Bedroom free bulk** | Maximize coverage, low cost | Chain: microsoft → optional openrouter/free; automation on; conservative age; batch off or small |
| **Quality AI** | Manual / selective | Primary OpenRouter/paid or local; automation off or low max/run; validation strict |
| **Safe first run** | New library | Automation off; dry-run on; age threshold high; deep scan off |
| **Debug** | Incident | log_level Debug; sync Debug; hangfire Warning |

Presets **never** overwrite API keys unless the preset explicitly includes empty placeholders and the user checks “replace secrets”.

UI: `Presets ▾` on Translation and Automation headers; show diff modal before apply.

---

## 7. Validation and safety

### 7.1 On save

- Connections: optional **Test** (HTTP to Sonarr/Radarr `/api/v3/system/status`).
- Chain: at least one row; warn if AI row has empty API key; warn if only free scrapers for large library.
- Automation: if enabled and max/run ≤ 0 → error; if age threshold 0 → warning (“will thrash new files”).
- Path maps: reject empty sides; warn if container path not visible (probe).

### 7.2 Health strip (System page)

Read-only status row:

- Sonarr/Radarr reachable  
- Translation queue depth / active jobs  
- Hangfire WAL size band (ok / large / critical)  
- Last automation cycle index + last success time  

### 7.3 Danger actions

All `action` types that delete data require:

1. Typed confirm string or checkbox  
2. Audit log line (even if only in app logs)  
3. No accidental double-submit  

---

## 8. UX details

1. **Importance folds** — Required always open; Recommended default open; Advanced collapsed unless search matches or user preference.
2. **Inline docs** — “Learn more” opens fork docs fragment (`docs/…`) or Lingarr.Docs path, not external walls of text.
3. **Env var parity** — Each field shows “Env: `SONARR_URL`” when applicable (from existing configuration table).
4. **Import / export** — Download/upload settings JSON (secrets redacted by default; optional include secrets with warning).
5. **Keyboard** — `/` focuses settings search; `S` saves when dirty.
6. **Mobile** — Bottom sheet for TOC; single-column cards; chain rows full-width (already mostly true).

---

## 9. Technical approach

### 9.1 Frontend

- Introduce `settingsCatalog.ts`: declarative field list → renderer `DynamicSettingField.vue` (reuse PluginField patterns).
- Refactor page shells to `SettingsPageLayout.vue` (search, TOC, sticky save, presets).
- Keep specialized editors for **ordered_chain** and **path_map** (too rich for generic controls).
- Setting store: track `dirtyKeys`, `defaults`, `searchIndex`.

### 9.2 Backend

- Additive keys only; seed defaults in FluentMigrator when needed.
- Optional `GET /api/setting/catalog` returns field metadata for UI and plugins.
- Action endpoints under `/api/system/...` or `/api/setting/actions/...` (clear history, reset cursors, wal checkpoint).
- Log level: apply to `ILoggingBuilder` filters at runtime where possible; else document restart.

### 9.3 Compatibility

- Old routes keep redirects (`/settings/services` → `/settings/translation?tab=chain`).
- `service_type` rich JSON unchanged.
- Env bootstrap in `StartupService` remains source of truth on first boot.

---

## 10. Implementation phases

### Phase 0 — Spec lock (0.5–1 d)

- Finalize group names, redirects, and first-wave keys.
- Capture screenshots of current Services/Subtitle/Automation for before/after.

### Phase 1 — Shell + IA (3–5 d)

- `SettingsPageLayout`, search, TOC, dirty bar.
- Reshuffle routes/nav; promote Mapping; redirect legacy paths.
- Move Translation card under Translation page; no new keys yet.

### Phase 2 — Field catalog + types (3–5 d)

- Shared field renderer + dependsOn.
- Migrate Integration + Automation to catalog-driven cards.
- Duration/enum/url test-connection for *arr.

### Phase 3 — QoL keys + actions (3–4 d)

- Retention, log levels, cursor reset, exclude dirs, scan mode.
- Maintenance status strip (WAL size, queue).
- Clear translation history action (confirm).

### Phase 4 — Presets + import/export (2–3 d)

- Three presets + diff apply modal.
- Export/import redacted JSON.

### Phase 5 — Polish (2 d)

- Mobile, a11y, empty states, docs links.
- Smoke tests: catalog load, save round-trip, preset apply, redirects.

**Rough total:** ~2–3 weeks calendar for one full-stack engineer familiar with the fork.

---

## 11. Success metrics

| Metric | Target |
|--------|--------|
| Time to find “path mapping” | &lt; 10 s for returning users (was: often never found) |
| Support questions “where is X setting?” | Down after one release cycle |
| Accidental automation stampede | Zero after dry-run + warnings ship |
| Settings save errors pre-job | Prefer validation at save over failed Hangfire jobs |
| Mobile settings task completion | Can edit chain + languages without horizontal overflow |

Qualitative: operators can describe Settings as “Connections → Translation → Automation → System” without a wiki.

---

## 12. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Large refactor breaks plugin forms | Share field renderer; keep Plugins page on old path until catalog stable |
| Route renames break bookmarks | Permanent redirects |
| Too many new keys confuse | Presets + Advanced fold; ship keys in waves |
| Action endpoints abuse | Auth required when auth enabled; confirm tokens |
| Scope creep into media UI | Strict non-goals; separate proposals |

---

## 13. Open questions

1. Should **Appearance** stay separate, or fold into System?
2. Do we expose Hangfire dashboard link in System for development images only?
3. Preset storage: hardcoded in client vs server-defined JSON for remote update?
4. Is **dry-run automation** worth full job pipeline hooks, or log-only first?
5. Upstream contribution: IA-only changes vs Bedroom-only ops keys?

---

## 14. Recommendation

**Approve Phase 1–2** as the near-term product bet: reorganize IA, promote Mapping, introduce layout/search/dirty-save, and catalog-driven simple fields.  

**Phase 3** should follow immediately for Bedroom (cursor reset, retention, log levels, maintenance strip)—these map directly to production pain (translation clutter, Hangfire WAL, automation memory).  

**Presets (Phase 4)** after the chain UI is stable so free-bulk vs quality-AI is one click instead of a checklist.

---

## 15. Doc maintenance

| When | Update |
|------|--------|
| Phase completes | Checkboxes / status at top of this file |
| New setting keys | `SettingKeys.cs` + this §5.3 table + Settings.MD if user-facing |
| Nav changes | `SettingPage.vue` routes + this §4 |

**Suggested branch name:** `feat/settings-ia-qol`  
**Deploy:** merge to `main` → fast-forward `bedroom` → rebuild `lingarr-bedroom` (never official GHCR).
