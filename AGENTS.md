# AGENTS.md — Bedroom Lingarr fork

Guidance for humans and coding agents working in **this** repo (`apoapostolov/lingarr`), not upstream.

## Source of truth

| Role | Location |
|------|----------|
| **Deploy / Docker image** | This fork only → image `lingarr-bedroom:*` |
| **Official upstream (import only)** | https://github.com/lingarr-translate/lingarr |
| **Local clone** | `/mnt/c/git-ext/lingarr` |
| **Ops skill** | Hermes `lingarr-local` (`references/bedroom-fork-build.md`) |
| **Product proposals** | `docs/` (keep in sync when behaviour changes) |

Never point Bedroom compose at official GHCR. Rebuild the bedroom image after meaningful server/client changes.

## Active proposal docs

- `docs/ai-providers-model-fallback-chain.md` — AI providers, model picker, provider+model fallback chain

When you change product behaviour covered by a proposal, **update that doc in the same PR/commit**.

## OpenRouter conventions (Bedroom)

- **Preferred default model:** `openrouter/free` (free metamodel).
- Intended use: bulk / low-quality-OK subtitle translation without burning paid credits.
- **Model dropdown order** (must stay true in `OpenRouterService.GetModels`):
  1. `openrouter/free` — always first; inject if catalogue omits it
  2. `openrouter/auto`
  3. remaining catalogue (label sort)
- Seed / default setting key `openrouter_model` → `openrouter/free` when empty on new installs.
- Do not reorder auto above free without an explicit product decision.

## Translation chain (`service_type`)

- Prefer rich JSON: `[{ "provider": "openrouter", "model": "openrouter/free" }, …]`
- Legacy plain string / string-array still parse.
- Same provider may appear twice with different models.
- Chain row `model` overrides global `*_model` via `IModelOverridable` / `ResolveModel`.

## Providers added on this fork

| Id | Notes |
|----|--------|
| `openrouter` | OpenAI-compat; free metamodel first |
| `zai` | **GLM Coding Plan only** — base `https://api.z.ai/api/coding/paas/v4` (not general `.../api/paas/v4`). Models: `glm-5.2`, `glm-5-turbo`, `glm-4.7`. Wrong endpoint → 1113 insufficient balance. |
| `opencode-go` | Implemented; may be unsubscribed in Bedroom |
| `deepseek` | Upstream-style + model override |

## Secrets

- API keys live as encrypted Lingarr settings (and may be seeded from lifestyle `.env` for ops).
- **Never** commit real keys, library paths, or real movie/TV/comic/ebook titles in issues, PRs, commits, or docs. Use placeholders.

## Build / deploy (Bedroom)

```bash
# from clone
docker build -f Lingarr.Server/Dockerfile -t lingarr-bedroom:latest \
  --build-arg TARGETARCH=amd64 --network=host .

docker compose -p media \
  -f /mnt/c/git/lifestyle/linux/dockhand/stacks/Bedroom/media/compose.yaml \
  up -d --force-recreate --no-deps lingarr
```

Or: `bash ~/.hermes/skills/devops/lingarr-local/scripts/build-bedroom-image.sh`

After recreate: re-check `SERVICE_TYPE` / plain vs JSON and that settings rows exist for new keys (Lingarr `SetSetting` does not create missing keys).

## Importing upstream

```bash
bash ~/.hermes/skills/devops/lingarr-local/scripts/sync-upstream-into-fork.sh
# review conflicts, then rebuild image
```

Do not auto-deploy after merge without a smoke check.

## Code style notes

- Prefer extending shared OpenAI-compatible patterns over copy-paste services.
- Model lists: go through `IModelCatalogService` (cache + `?refresh=true`).
- UI: model select sits **between** provider and API credentials on Services settings.
- Public OSS text (if any): no personal media fingerprints — see Hermes `gh-local` skill.

## Testing checklist (AI providers)

1. `GET /api/plugin` includes `openrouter`, `zai`, `opencode-go`, `deepseek`
2. `GET /api/plugin/openrouter/models` → first option value is `openrouter/free`
3. Translate line smoke for providers with working keys
4. Restore `service_type` to a free scraper (e.g. microsoft) after paid tests if desired
