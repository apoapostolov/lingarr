# Bedroom Lingarr reliability — development plan

**Date:** 2026-07-26  
**Source:** Docker audit of container `lingarr` (`lingarr-bedroom:latest`) + fork `/mnt/c/git-ext/lingarr`  
**Goal:** Fix production failures observed under Bedroom compose (`mem_limit: 512m`, SQLite Hangfire, free Microsoft translator, large library).

---

## Context

| Signal | Approx. count | Meaning |
|--------|---------------|---------|
| `SQLiteException: file is not a database` | ~2 950 | Hangfire SQLite lock/WAL failure spiral |
| Hangfire server “considered dead” | 65 | Job server restart loop under storage/memory pressure |
| `Cannot allocate memory` in `GetAllSubtitles` | 28 | Recursive directory scan under 512 MiB |
| Microsoft timeout / connection reset | dozens | Free translator flakiness fails whole jobs |
| “No valid source language” (already `bg`) | 205 | Expensive scan before skip |
| Hangfire WAL file | **~1 GiB** | WAL never checkpointed despite ~99 jobs |

Deploy image: fork-built `lingarr-bedroom:*` only (never official GHCR). Config: `/srv/docker/lingarr/config`.

---

## Priority workstreams

### P0 — Subtitle filesystem scanning

**Problem:** `SubtitleService.GetAllSubtitles` uses `Directory.GetFiles(..., AllDirectories)`, materializing every subtitle under season/movie trees (including `Trailers/`), which OOMs the cgroup.

**Plan:**
1. Prefer top-level enumeration next to media files.
2. Allow only known subtitle subdirs (`Subs`, `Subtitles`, …).
3. Skip junk directories (`Trailers`, `Featurettes`, `Sample`, …).
4. `GetSubtitles(path, fileName)` must match by filename pattern without loading the whole tree.
5. Catch `IOException` (ENOMEM) and surface as skip-friendly errors.

**Files:** `Lingarr.Server/Services/SubtitleService.cs`, tests.

---

### P0 / P1 — Automation job memory + durable cursor

**Problem:** `AutomatedTranslationJob` loads all included movies/episodes into memory (25k+ episodes) and stores the cycle index only in `IMemoryCache` (resets to 0 on restart).

**Plan:**
1. Query with EF `Skip`/`Take` windows; never materialize full library.
2. Persist movie/show processing indices via settings upsert (`automation_movie_processing_index`, `automation_show_processing_index`).
3. On ENOMEM / directory IO failures: log warning, continue cycle.
4. Keep cycle semantics: scan until `MaxTranslationsPerRun` initiated or one full pass.

**Files:** `AutomatedTranslationJob.cs`, `SettingKeys.cs`, `ISettingService` / `SettingService`.

---

### P0 — Hangfire SQLite health

**Problem:** Hangfire.Storage.SQLite + WAL produces multi‑GB WAL and historical “file is not a database” lock spam; servers die non-gracefully.

**Plan:**
1. Startup: integrity probe; if corrupt, quarantine `Hangfire.db*` and recreate empty store.
2. Apply WAL pragmas + busy timeout (already partial).
3. Hosted service: periodic `PRAGMA wal_checkpoint(TRUNCATE)` (default every 15 min).
4. Document that Postgres/MySQL remain preferred for large installs; SQLite path is hardened for Bedroom.

**Files:** `ServiceCollectionExtensions.cs`, new `HangfireSqliteMaintenanceService.cs`.

**Ops companion (not in-app):** checkpoint or delete WAL under `/srv/docker/lingarr/config` after deploy; fix root ownership so maintain script can VACUUM.

---

### P1 — Microsoft / GTranslate resilience

**Problem:** Timeouts and connection resets throw immediately as “unexpected” without retry; 300s client timeout ties the single worker.

**Plan:**
1. Retry transient errors (timeout, connection reset, `HttpRequestException` without permanent status) with exponential backoff + jitter.
2. Honor caller `CancellationToken` for shutdown.
3. Leave multi-provider fallback to existing feature branches; this change makes single-provider mode survivable.

**Files:** `GTranslatorService.cs`.

---

### P2 — Cheap skips, logging, bad IDs

**Plan:**
1. Demote “no source language / already has target only” to **Debug**.
2. Demote per-item sync “Syncing episode/movie” to **Debug**; keep batch summaries at Information.
3. Reject Radarr/Sonarr id `<= 0` before HTTP call.
4. Optional: Hangfire package’s own `!!!` console noise is third-party; cannot fully silence without package change.

**Files:** `MediaSubtitleProcessor.cs`, `SeasonSync.cs`, `MovieSync.cs`, `MediaService.cs`.

---

## Out of scope (follow-ups)

- Switching Bedroom Hangfire to Postgres (recommended long-term; compose change).
- Raising `mem_limit` alone without scan fixes.
- Full AI provider fallback chain (separate feature branch).
- Content-API subtitle path feature work (other branch).

---

## Acceptance criteria

- [ ] No recursive full-tree subtitle materialization on normal media folders.
- [ ] Automation does not load entire episode table into memory.
- [ ] Processing indices survive process restart.
- [ ] Hangfire SQLite: startup recovery + periodic WAL checkpoint.
- [ ] Transient Microsoft failures retry with backoff.
- [ ] `movieId`/`episodeId` ≤ 0 short-circuit.
- [ ] Sync/automation “already done” paths are not Warning spam.
- [ ] Unit tests for subtitle scan + automation cursor/window behavior.
- [ ] `dotnet test` on Server.Tests green (when SDK available).

---

## Deploy checklist (Bedroom)

```bash
# After merge to bedroom/main:
bash ~/.hermes/skills/devops/lingarr-local/scripts/build-bedroom-image.sh
COMPOSE=/mnt/c/git/lifestyle/linux/dockhand/stacks/Bedroom/media/compose.yaml
docker compose -p media -f "$COMPOSE" stop lingarr
# Optional reclaim if WAL still huge:
# sqlite3 /srv/docker/lingarr/config/Hangfire.db 'PRAGMA wal_checkpoint(TRUNCATE);'
# or move Hangfire.db* aside to force recreate
docker compose -p media -f "$COMPOSE" up -d --force-recreate --no-deps lingarr
docker logs -f lingarr
```

---

## Implementation status

Implemented on branch `fix/bedroom-reliability-audit`.

| Item | Status |
|------|--------|
| Plan doc | done — this file |
| Subtitle scan (no AllDirectories; exclude Trailers; allow Subs) | done — `SubtitleService` |
| Automation paging + durable index | done — `AutomatedTranslationJob` + `UpsertSetting` |
| Hangfire corrupt recovery + startup/periodic WAL checkpoint | done — `HangfireSqliteMaintenanceService` |
| GTranslate transient retry + jitter | done — `GTranslatorService` |
| Radarr/Sonarr id ≤ 0 guard | done — `MediaService` |
| Log demotion (sync Debug; already-translated Debug) | done |
| Tests | **203 passed** (`Lingarr.Server.Tests`) |

**Env (optional):** `HANGFIRE_WAL_CHECKPOINT_MINUTES` (default `15`).
