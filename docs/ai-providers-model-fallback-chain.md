# Proposal: AI providers, model picker, and provider+model fallback chain

**Status:** Implemented on bedroom fork (iterate in-branch)  
**Branch:** `feat/ai-providers-model-fallback-chain`  
**Deploy:** build `lingarr-bedroom` from this branch / `bedroom` after merge  
**Official reference (import only):** https://github.com/lingarr-translate/lingarr  
**Bedroom fork:** https://github.com/apoapostolov/lingarr  
**Agent guide:** [`AGENTS.md`](../AGENTS.md) — keep this proposal and AGENTS.md updated together

## Goals

1. Full first-class providers: **OpenRouter**, **Z.ai (GLM)**, **DeepSeek** (polish), **OpenCode Go**
2. **Cached model lists** with refresh
3. **Model dropdown** between provider selector and API key fields
4. **Fallback chain** as ordered **provider + model** rows (+ add, − remove, reorder), not provider-only

## Personas

- Frontend Engineer + UI/UX Designer (settings IA, chain editor, model select)
- Full-stack for factory, parse migration, catalog cache, OpenAI-compat services

## Design decisions

1. **Single source of truth:** extend `service_type` to a JSON array of objects; migrate legacy string arrays on read/write.
2. **Allow same provider twice** with different models (required for useful AI fallbacks).
3. **Services page = two cards:** Primary + Fallback chain (dedicated Fallbacks child route optional later).
4. **Model row** only for multi-model / AI providers; hidden for NMT scrapers (microsoft/google/…).
5. **Chain row model overrides** global `*_model` setting at translate time.
6. **Shared OpenAI-compatible base** where practical (OpenRouter / Z.ai / OpenCode Go / DeepSeek patterns).
7. **Model catalog cache** server-side (memory), TTL ~6h, `?refresh=true` bypass.
8. **Docker:** only fork-built `lingarr-bedroom` — never official GHCR for Bedroom.
9. **OpenRouter default metamodel:** `openrouter/free` is first in the model dropdown and the preferred Bedroom default for bulk / low-quality-OK subtitle work (free router). `openrouter/auto` is second. Paid catalogue models follow. Always inject `openrouter/free` if the API catalogue omits it.

## `service_type` schema

Legacy (still accepted):

```json
["microsoft", "deepseek"]
```

Target:

```json
[
  { "provider": "openrouter", "model": "openrouter/free" },
  { "provider": "deepseek", "model": "deepseek-chat" },
  { "provider": "microsoft" }
]
```

Plain string `microsoft` (Bedroom compose env) still parses as a one-entry chain.

## Providers

| Id | Notes |
|----|--------|
| `deepseek` | Existing + model override + catalog cache |
| `openrouter` | OpenAI-compat; **dropdown order: free → auto → rest**; seed model `openrouter/free` |
| `zai` | OpenAI-compat GLM; general API base (coding plan URL optional) |
| `opencode-go` | OpenAI-compat; implement even if Bedroom subscription is inactive |

## OpenRouter model UX (locked)

```
Model [ openrouter/free • Free metamodel (default for bulk) ▾ ]  ← position 0
      [ openrouter/auto • Auto router                       ]
      [ … paid / other catalogue …                          ]
```

- Use free for low-quality / bulk path when cost matters.
- Higher quality → pick a paid model or chain free → paid fallback later if desired.

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

## Implementation status

| Phase | Scope | Status |
|-------|--------|--------|
| P0 | Parse/normalize rich chain; factory + model override; allow duplicate providers | Done |
| P1 | Model catalog cache; Services UI primary+fallbacks with model column | Done |
| P2 | OpenRouter port + **free-first ordering** | Done |
| P3 | Z.ai + OpenCode Go | Done (OpenCode Go untested if unsubscribed) |
| P4 | Polish empty states, automation warnings, docs/AGENTS | In progress |

## Out of scope

- Per-row temperature/max-tokens (optional follow-up)
- Auto-merge from official upstream without human review
- Publishing to official GHCR

## Security / ops

- API keys remain encrypted settings
- Automation cost warnings for paid AI
- After ship: `build-bedroom-image.sh` + recreate container; re-check plain/env `SERVICE_TYPE` migration
- Setting keys must be seeded (empty row) before encrypted set works

## Related files

- `AGENTS.md` — agent/human operating rules for this fork
- `Lingarr.Server/Services/Translation/OpenRouterService.cs` — free-first catalogue
- `Lingarr.Server/Services/Translation/TranslationChain.cs` — parse/serialize chain
- `Lingarr.Client/src/components/features/settings/ServicesSettings.vue` — UI
- Hermes skill `lingarr-local` for Bedroom rebuild/import
