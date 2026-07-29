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
- **Deployed:** `lingarr-bedroom:latest` with Qwen General, Qwen Translation,
  xAI API, and experimental xAI SuperGrok/Premium+ support.
- **Protected lines:** `main` and `next`.
- **Validation baseline:** client production build, Docker server image build,
  271 server tests, provider manifest/model endpoint smoke checks, and healthy
  live container.

## Next decisions

### Microsoft long-line handling

- Decide whether to port the retained `fix/microsoft-translate-long-lines`
  chunking behavior onto the current provider architecture.
- Do not merge the old branch wholesale; preserve current retry, timeout, and
  vector-drawing behavior.
- Stop after a focused regression test and a provider-safe smoke check.

### Branch and review hygiene

- Resolve or close the remaining old PR anchors before deleting their branches:
  content API paths and the obsolete request-timeout PR.
- Keep protected release lines and any branch containing unique, unported work.

## Definition of done

- The requested behavior exists in its canonical owner.
- Relevant tests and documentation are updated.
- Client/server builds and the narrow smoke path pass, or the gap and risk are
  recorded explicitly.
- Migration and deployment effects are understood.
- The handoff names the exact commit, validation, deployment state, and follow-up
  decision without claiming work that was not performed.
