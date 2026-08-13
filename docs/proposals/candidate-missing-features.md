# Proposal: Candidate missing features

**Status:** Research — ideas only; nothing approved or implemented

**Scope:** Gaps in the current Lingarr Next product surface, identified by studying the translation pipeline, automation, media handling, and data model

**Related:** [settings-page-ia-and-qol.md](./settings-page-ia-and-qol.md), [translation-confidence-and-instruction-profiles.md](./translation-confidence-and-instruction-profiles.md), [feature-bloat-governance.md](./feature-bloat-governance.md), [../architecture.md](../architecture.md)

**Audience:** Product contributors deciding what (if anything) to build next

---

## How to read this

These are **candidate** features found by tracing the actual product behaviour,
not a wishlist. Each entry names the concrete gap, why it matters, and a
deliberately small first shape. Nothing here is approved. The
[feature-bloat governance proposal](./feature-bloat-governance.md) applies to
all of them: prefer a safe default over a control, define failure states, and
stay inside the bloat budget before building.

Items are ordered by how clearly the gap is grounded in today's behaviour.

---

## Tier 1 — Gaps grounded directly in current behaviour

### 1. Translation glossary / name preservation

**Gap.** There is no glossary. Character names, place names, and fixed terms
are re-translated line-by-line with no memory, so the same proper noun can
come out three different ways in one file. AI providers get only the current
line(s) in context.

**Why it matters.** Inconsistent names are the most visible quality defect in
subtitle translation, and the existing quality layer
(`TranslationQualityAssessment`) already flags "consistency" findings — but it
can only *report* the problem, never *prevent* it.

**Small first shape.** A per-language-pair glossary (source term → target
term) injected into AI providers' existing prompt/context pipeline, and applied
as a post-translation replacement pass for all providers. Per-library or
per-show glossaries come later, only if global is insufficient.

**Leverages:** `AI_CONTEXT_PROMPT`, the instruction-profile versioning system,
`BaseLanguageService._replacements` (a replacement dictionary already exists
internally).

---

### 2. Smarter source-subtitle selection

**Gap.** `SubtitleService.SelectSourceSubtitle` picks the **first** subtitle
whose language matches a configured source language
(`availableLanguages.FirstOrDefault(...)`). It has no preference order and no
quality signal — if both `en` and `en-SDH` exist, the choice is arbitrary, and
the operator's source-language list order is not honoured as a priority.

**Why it matters.** Wrong source selection silently degrades every downstream
translation. This is already partially mitigated by `ignoreCaptions`, but only
as a skip rule, not a ranking.

**Small first shape.** Treat the configured `source_languages` list as an
**ordered preference**, and prefer non-caption tracks over caption tracks of
the same language. No new setting — it makes the existing list mean what users
already assume it means.

**Leverages:** existing `source_languages` setting and `ignoreCaptions` rule.

---

### 3. Re-translation on demand (manual refresh)

**Gap.** Once a target language exists, automation skips it
(`languagesToTranslate.Except(existingTranslationRequests)`). The only way to
re-translate is to delete the old request/file or wait for the cleanup window.
There is no "translate this again, in a different/better model" action on a
media row.

**Why it matters.** Operators who improve their provider chain (e.g. switch to
a better model, add a glossary) have no clean way to re-run existing targets.
Today they must delete history — which the cleanup job also does automatically
after 7 days, losing the audit trail.

**Small first shape.** A per-media "Re-translate" action that creates a new
request for an existing target language and supersedes (not deletes) the prior
file/request. Superseded requests stay visible in history.

**Leverages:** existing `TranslationRequest` lifecycle, the retry action that
already exists for failed requests.

---

### 4. Cost budget / spend guard

**Gap.** `ProviderOperationalEvent` already records `InputTokens`,
`OutputTokens`, and `EstimatedCostUsd` for metered providers, and the Dashboard
shows recent spend. But nothing **stops** spend — there is no daily/monthly cap
that pauses metered translation when a threshold is reached.

**Why it matters.** A misconfigured automation run against a paid model on a
large library can spend real money before anyone notices. The data to prevent
this already exists; only the guard is missing.

**Small first shape.** A single optional monthly cost ceiling. When exceeded,
metered providers are skipped (falling through the chain to free/scrape
providers) and the Dashboard surfaces a "budget reached" state. Default:
**off** (unlimited), so behaviour is unchanged until an operator opts in.

**Leverages:** existing metered-usage tracking and the provider fallback chain.

---

## Tier 2 — Clearly useful, slightly more speculative

### 5. Per-media / per-library translation overrides

**Gap.** Include/exclude and age thresholds are global or per-media-type.
There is no way to say "translate this one show into Japanese too" or "never
touch this library" without changing global target languages.

**Why it matters.** Operators with mixed-language households or special
collections currently have no scoped control; they work around it by toggling
global settings.

**Small first shape.** Per-media "extra target language" and per-library
"exclude from automation" toggles on the existing media rows. Reuses the
existing include/exclude affordance, just scoped.

**Leverages:** existing `includeInTranslation` flag on media entities.

---

### 6. Scheduled / off-hours translation window

**Gap.** Automation runs on a cron schedule, but there is no "only run during
these hours" constraint. A free-provider metamodel may be fine anytime, but a
metered model might be wanted only overnight.

**Why it matters.** Couples cost (Tier 1 #4) and scheduling, and reflects how
people actually want to use automation.

**Small first shape.** Optional active-hours window on the automation master
switch. Outside the window, the translation job is skipped (indexing still
runs). Default: always active.

**Leverages:** existing `translation_schedule` and the schedule service.

---

### 7. Translation diff / side-by-side review

**Gap.** `TranslationDetailPage` shows a request and its lines, and quality
findings annotate lines. But there is no source-vs-target side-by-side view to
manually review a translation before trusting it.

**Why it matters.** The quality score is mechanical; for languages the
operator reads, a quick visual diff is faster than trusting a number.

**Small first shape.** A read-only side-by-side mode on the existing detail
page using the already-stored `TranslationRequestLine.Source` and `.Target`.

**Leverages:** `TranslationRequestLine` already persists both source and target.

---

## Tier 3 — Worth considering, more design needed

### 8. Source-language auto-detection

**Gap.** Source language is matched purely from the configured list and the
filename. Files with missing/ambiguous language tags never match and are
silently skipped.

**Why it matters.** Improves discoverability of translatable media, but adds a
detection dependency and edge cases.

**Small first shape.** A lightweight heuristic on subtitle *content* (common-stopword
ratio) as a fallback when filename matching fails. Off by default; surfaced as
a "detected" badge.

---

### 9. Backup / export of translated subtitles

**Gap.** Translated files are written next to media and tracked in the DB, but
there is no export/archive of completed translations independent of the media
folder.

**Why it matters.** Useful for disaster recovery or migrating libraries, but
overlaps with whatever the media server already does.

**Small first shape.** A "download all completed translations for this media"
action. Full-library archive deferred unless requested.

---

### 10. Notification channels beyond the in-app toast

**Gap.** Notifications exist in-app; there is no push to email/webhook/NTFY on
completion or failure of a run.

**Why it matters.** Operators running unattended automation want to know when
it fails without keeping a tab open.

**Small first shape.** Optional webhook URL on job completion/failure, reusing
the existing `WebhookJob` plumbing (which currently handles *inbound* Radarr/
Sonarr webhooks).

---

## What this list is not

This file is a research backlog, not a forbid-list. New providers, optional AI
revision, and later quality enforcement are allowed when they earn a place and
are designed in Lingarr Next's UI language. Upstream features are ingested as
backend/behavior, then given a Next frontend — they are not merged as-is.

Still a poor default unless a later proposal asks for them:

- A plugin marketplace UI. Manifests already cover provider discovery.
- Asking an LLM to grade every translation automatically.
- Arbitrary log-level or Hangfire WAL controls in Settings.
- Custom Dashboard date ranges and per-card toggles.

---

## Recommended next step

If any of these are worth pursuing, **Tier 1 #2 (smarter source selection)**
has the best ratio of value to risk: it changes no settings, adds no UI, and
makes existing behaviour match user expectations. It is also the smallest to
validate with a unit test against `SelectSourceSubtitle`.

Everything else should pass the feature-bloat governance gate (object, default
vs. control, failure states, budget impact, removal cost) before a PR.
