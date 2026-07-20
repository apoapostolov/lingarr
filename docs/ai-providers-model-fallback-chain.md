# Proposal: AI providers, model picker, and provider+model fallback chain

**Status:** Accepted for implementation on bedroom fork  
**Branch:** `feat/ai-providers-model-fallback-chain`  
**Deploy:** build `lingarr-bedroom` from this branch after merge to `bedroom`  
**Official reference (import only):** https://github.com/lingarr-translate/lingarr  
**Bedroom fork:** https://github.com/apoapostolov/lingarr

## Goals

1. Full first-class providers: **OpenRouter**, **Z.ai (GLM)**, **DeepSeek** (already present — polish), **OpenCode Go**
2. **Cached model lists** with refresh
3. **Model dropdown** between provider selector and API key fields
4. **Fallback chain** as ordered **provider + model** rows (+ add, − remove, reorder), not provider-only

## Personas

- Frontend Engineer + UI/UX Designer (settings IA, chain editor, model select)
- Full-stack for factory, parse migration, catalog cache, OpenAI-compat services

## Current state (bedroom `main`)

| Item | Reality |
|------|---------|
| DeepSeek / OpenAI / Anthropic / Gemini / LocalAI | Implemented with plugin manifests + `RemoteDropdown` models |
| OpenRouter | On fork branch `feature/openrouter-1.2.1` — port into this work |
| Fallback chain UI | `ServicesSettings.vue`: Primary + Fallback N, unique providers only |
| `service_type` storage | JSON string array of provider ids; runtime walks list in `SubtitleTranslationService` |
| Per-row model | Missing — model is global per provider setting |

## Design decisions

1. **Single source of truth:** extend `service_type` to a JSON array of objects; migrate legacy string arrays on read/write.
2. **Allow same provider twice** with different models (required for useful AI fallbacks).
3. **Services page = two cards:** Primary + Fallback chain (dedicated Fallbacks child route optional later).
4. **Model row** only for multi-model / AI providers; hidden for NMT scrapers (microsoft/google/…).
5. **Chain row model overrides** global `*_model` setting at translate time.
6. **Shared OpenAI-compatible base** where practical (OpenRouter / Z.ai / OpenCode Go / DeepSeek patterns).
7. **Model catalog cache** server-side (memory + optional disk), TTL ~6h, `?refresh=true` bypass.
8. **Docker:** only fork-built `lingarr-bedroom` — never official GHCR for Bedroom.

## `service_type` schema

Legacy (still accepted):

```json
["microsoft", "deepseek"]
```

Target:

```json
[
  { "provider": "openrouter", "model": "anthropic/claude-sonnet-4" },
  { "provider": "deepseek", "model": "deepseek-chat" },
  { "provider": "microsoft" }
]
```

Plain string `microsoft` (Bedroom compose env) still parses as a one-entry chain.

## Providers

| Id | Notes |
|----|--------|
| `deepseek` | Existing; ensure model UI order + cache |
| `openrouter` | Port from `feature/openrouter-1.2.1` |
| `zai` | OpenAI-compat; endpoint mode general vs coding plan; GLM models |
| `opencode-go` | OpenAI-compat OpenCode Go catalog; API key + model list |

## UI layout

```
Primary service
  Provider [ … ▾ ]
  Model    [ … ▾ ] [↻]     ← between provider and API (AI only)
  API key  [ … ]
  (extra fields…)

Fallback chain
  #1 Provider [ … ] Model [ … ] ↑ ↓ ✕
  #2 …
  [ + Add fallback ]
```

## Phases

| Phase | Scope |
|-------|--------|
| P0 | Parse/normalize rich chain; factory + model override; allow duplicate providers |
| P1 | Model catalog cache; Services UI primary+fallbacks with model column |
| P2 | OpenRouter port |
| P3 | Z.ai + OpenCode Go |
| P4 | Polish empty states, automation warnings, docs |

## Out of scope (this branch)

- Per-row temperature/max-tokens (optional follow-up)
- Auto-merge from official upstream without human review
- Publishing to official GHCR

## Security / ops

- API keys remain encrypted settings
- Automation cost warnings for paid AI
- After ship: `build-bedroom-image.sh` + recreate container; re-check plain/env `SERVICE_TYPE` migration

## Implementation notes

- Official repo URL for future ports: https://github.com/lingarr-translate/lingarr
- Bedroom skill: `lingarr-local` → `references/bedroom-fork-build.md`
