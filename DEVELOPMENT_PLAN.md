# Lingarr Next development plan

This is the current execution map for AI-assisted development. It is not a
replacement for product proposals or architecture documentation:

- `docs/` holds durable design rationale and behavior contracts.
- `CHANGELOG.md` holds user-facing release history.
- this file holds only active sequencing, decisions, validation, and follow-up.

## Planning contract

Every non-trivial change starts with a compact plan containing:

1. Goal and observable user outcome.
2. Non-goals and compatibility constraints.
3. Canonical files or services that own the behavior.
4. Ordered implementation steps and the migration/deployment impact.
5. Validation commands and a clear stop condition.

The plan is updated when scope or sequencing changes, not used as a running
transcript. Completed work belongs in the changelog or a dated development log.

## Current state

- **Released:** Lingarr Next 1.0.1, “Moar Providers” (`855caec`).
- **Fork main:** tracks official upstream 1.3.0. Do not merge `main` into `next`.
- **next:** Lingarr Next plus portable 1.3.0 fixes, Microsoft long-line
  chunking, Mistral, AI revision, and weekly subtitle housekeeping.
- **Protected lines:** `main` and `next`.
- **Validation baseline:** server unit tests including chunker + Local AI parse
  retry. Image rebuild/deploy is a follow-up, not part of this import.

## Next decisions

### Remaining 1.3.0 features (not ported)

- Upstream xAI is a weaker copy of Next's xAI + OAuth. Leave it.
- Upstream user-prompt rewrite is a weaker copy of instruction profiles. Leave it.
- Dependabot bumps from 1.3.0: safe NuGet 10.0.10 train + axios/vue/vite/vue-tsc
  taken. Still skipped: Pinia 4, `@types/node` 26, Tailwind 4.3, oxlint/oxfmt.
- Upstream proofread Vue was not copied; Next uses the detail-page revise card.

### Branch and review hygiene

- `fix/microsoft-translate-long-lines` is now ported onto `next`. The old
  branch can be deleted after a remote check.
- Resolve or close the remaining old PR anchors before deleting their branches:
  content API paths and the obsolete request-timeout PR.

## Definition of done

- The requested behavior exists in its canonical owner.
- Relevant tests and documentation are updated.
- Client/server builds and the narrow smoke path pass, or the gap and risk are
  recorded explicitly.
- Migration and deployment effects are understood.
- The handoff names the exact commit, validation, deployment state, and follow-up
  decision without claiming work that was not performed.
