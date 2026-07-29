# Changelog

All notable changes to Lingarr@Bedroom are documented here.

Lingarr@Bedroom uses an independent version line. Its versions are not intended
to sort before or after versions published by upstream Lingarr.

## [1.0.0] - 2026-07-29

This is the first consolidated release of **Lingarr@Bedroom**, the separate fork
maintained by Apostol Apostolov.

### Dashboard and translation confidence

- Added a redesigned operational Dashboard centered on useful, recent
  information instead of lifetime-only totals.
- Restored Lingarr's readable historical translation graph inside Recent
  Activity.
- Added a configurable recent-activity window, normally 48 hours.
- Added a natural-language summary of completed subtitle files, quality results,
  provider contribution, active work, fallbacks, and provider availability.
- Added input-token, output-token, and approximate-cost reporting when metered
  OpenRouter, OpenAI, DeepSeek, or Anthropic translations occur in the selected
  period.
- Added a remembered primary-language selector to Media Overview. Movie and TV
  bars now describe current subtitle coverage in that language.
- Added Provider Health with explicit operational states, persistent structured
  outcomes, last-success information, expandable diagnostics, and safe provider
  tests.
- Added observe-only subtitle quality assessments from 0 to 100, including
  versioned line findings for suspicious output, preservation, language,
  formatting, readability, and consistency problems.
- Integrated quality results below Recent Activity and into completed
  translation status without blocking or deleting output.

### Translation providers and fallback chains

- Added first-class OpenRouter support with live model discovery and
  provider-reported pricing where available.
- Made `openrouter/free` the default and first model, followed by
  `openrouter/auto`, then the remaining model catalogue.
- Added Z.ai support for the GLM Coding Plan endpoint and its supported GLM
  models.
- Added OpenCode Go support and improved DeepSeek model handling.
- Added a cached model catalogue with manual refresh.
- Replaced provider-only fallback configuration with ordered provider-and-model
  rows.
- Allowed the same provider to appear more than once with different models.
- Preserved compatibility with legacy plain-string and string-array
  `SERVICE_TYPE` settings.
- Added provider-specific request timeouts, including a longer Microsoft default
  for slow free-translation requests.

### AI instruction profiles

- Added separate System Prompt and Context Prompt libraries.
- Added named drafts, immutable published versions, activation, rename,
  duplication, deletion, and version history.
- Added per-provider-row profile assignment with inheritance from active
  defaults.
- Added prompt-capability metadata to plugin manifests.
- Added a comprehensive example translation instruction set covering role,
  priorities, dialogue tone, profanity, forms of address, glossary, captions,
  numbers, readability, ambiguity, and final checks.
- Preserved existing prompt placeholders and added clearer guidance for
  dialogue-context tags.

### Settings and interface

- Reorganized Settings into Connections, Translation, Automation, System, and
  Plugins while preserving Lingarr's existing cards and control vocabulary.
- Added local Translation tabs for Setup, Subtitles, Prompts, and Advanced.
- Placed Translation Services before Languages and clarified ordered fallback
  editing.
- Added shared tab and settings-shell components for consistent responsive
  navigation.
- Removed anonymous telemetry from Settings, onboarding, scheduled jobs,
  endpoints, persistence, and client services.
- Pointed update checks to the Apostol Apostolov fork.
- Improved toast visibility with the theme's Development-pill styling, a
  restrained spring animation, and protection against notification floods.
- Fixed stale dynamically imported pages after deployments by using safe update
  and asset-path behavior.

### Translation list and detail views

- Added the episode name as a smaller second line beneath TV episode filenames.
- Replaced source and target pills with quieter rounded-square language markers.
- Moved completed quality into Status as compact `Quality: XX%` text.
- Centered Source, Target, Status, Progress, and Completed presentation where
  appropriate.
- Replaced strict completed dates with compact relative times such as `30 m` and
  `7 d`.
- Refined card, badge, and progress colors to remain compatible with every
  Lingarr theme.
- Added optional subtitle paths to content-translation API requests and made
  content titles optional.
- Hardened cross-platform basename handling in the Translations list.

### Reliability and operations

- Reworked subtitle discovery to avoid unbounded recursive scans, exclude
  trailer folders, and continue supporting conventional subtitle folders.
- Added paged automated-library processing with a durable cursor so large
  libraries make steady progress across runs and restarts.
- Added Hangfire SQLite startup recovery and periodic WAL checkpoint
  maintenance.
- Added transient retry with jitter for Microsoft/GTranslate requests.
- Added early rejection of invalid Sonarr and Radarr identifiers.
- Reduced noisy routine sync and already-translated messages to debug-level
  logging.
- Added safe provider operational retention and health reconciliation.
- Fixed fresh MySQL migrations for provider settings and imported prompt
  profiles by using database-correct quoting for the reserved settings key.
- Kept API keys encrypted and excluded subtitle text, prompts, paths, titles,
  and credentials from provider-health and usage records.

### Documentation and testing

- Added fork-specific architecture, testing, reliability, settings, provider,
  fallback-chain, confidence, Dashboard, and prompt-profile documentation.
- Added a live API smoke suite for provider manifests, free translation paths,
  model ordering, content requests, update checks, and health endpoints.
- Added regression coverage for translation chains, fallbacks, model catalogues,
  provider health, quality scoring, prompt profiles, automation, subtitle
  enumeration, Dashboard activity, LLM pricing, and database migrations.
- Added operator guidance that keeps Bedroom deployments on fork-built images
  and treats upstream as a selective import source.

### Release and compatibility notes

- Reset the fork's public release history to the independent `1.0.0` baseline.
- Moved the supported public image to
  `ghcr.io/apoapostolov/lingarr:1.0.0`.
- Database migrations through the LLM-usage and Dashboard-language additions are
  applied automatically at startup.
- Back up the application config and database before switching from an upstream
  image or attempting a downgrade.

[1.0.0]: https://github.com/apoapostolov/lingarr/releases/tag/1.0.0
