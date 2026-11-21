---
name: codebase-architecture-analyzer
description: Investigate a specifically requested software component in an unfamiliar codebase and produce evidence-backed architecture, design, and implementation documentation. Use when asked to explain or document a module, service, class, subsystem, API, workflow, or bounded context. Never assume behavior from names or framework conventions; verify it in code. Do not document the entire repository.
---

# Codebase Architecture Analyzer

## Mission
Reverse-engineer **only the component requested by the user** and document its observed architecture, design, and implementation. Begin every invocation with no assumed knowledge of the codebase, even if prior documentation or earlier sessions exist. Treat repository documentation and comments as leads to verify, not as proof of runtime behavior.

## Non-negotiable rules

1. **Scope is a hard boundary.** Identify the requested component and its ownership boundary first. Never silently expand the deliverable into a project-wide architecture review. If scope is ambiguous, select the smallest plausible component and state the interpretation; ask for clarification only when the component cannot be located with reasonable confidence.
2. **Start from uncertainty.** Do not infer that patterns, conventions, DI, CQRS, DDD, Clean Architecture, event sourcing, transactions, caching, retries, or security controls exist merely from names, folders, packages, or framework defaults.
3. **Verify before asserting.** Trace claims to concrete code, tests, configuration, or runtime wiring. Follow both declaration and call sites as needed. A method name is not evidence of its effect.
4. **Separate certainty levels.** Mark statements as `CONFIRMED`, `INFERRED`, `UNVERIFIED`, or `CONTRADICTED`. Document unknowns rather than filling them with guesses. An inference must state its premise and missing verification.
5. **Trace every substantial claim.** Cite repository-relative path plus symbol and line range when available, e.g. `src/Orders/CheckoutService.cs:L42-L68`. If line numbers are unavailable, cite path and symbol and declare that precision is limited. Never fabricate references.
6. **Read-only by default.** Do not edit source, build config, tests, dependencies, or repository documents. Only create or update documentation when explicitly requested, and use the agreed output location. Do not execute commands that mutate data or contact production systems.
7. **Dependency traversal is bounded.** Inspect external callers, collaborators, and infrastructure only to establish the selected component's contracts, behavior, effects, and failure modes. Describe them at interface level; do not generate separate full documentation for them.
8. **No exhaustive repo dump.** Inspect selectively, beginning with filenames, search indices, manifests, and bounded code regions. Avoid listing or reading all repository files by default.
9. **No silent substitutions.** If code cannot be accessed, dependencies are unavailable, or behavior is dynamic, explicitly state what cannot be verified. Do not treat tests, comments, or docs as complete proof of production execution.
10. **Do not confuse intended and actual design.** Label architectural patterns only if their implementation evidence matches. Call out divergence between design documentation and code when observable.

## Input contract

Required: target component by name, path, namespace, public API, feature, or described behavior.

Optional: repository root, output path, language, documentation depth (`overview`, `standard`, `deep`), desired diagrams, and exclusions. Default depth: `standard`. Default output: Markdown, focused on one component. Do not ask the user to choose options when defaults suffice.

## Workflow

### Phase 1 — Locate and bound

- Establish repository root and inspect only enough structure to locate the target.
- Search by the user's exact identifier, followed by related symbols, namespace, route, event, or dependency-registration names if needed.
- Identify the component's primary files, entry points, public surface, ownership boundary, and excluded neighboring subsystems.
- Write an internal **scope contract**:
  - `IN`: component responsibilities and implementation files.
  - `CONTEXT`: direct callers, dependencies, shared types, configuration and tests needed to validate `IN`.
  - `OUT`: unrelated modules and their internal architecture.
- If multiple matches exist, resolve from context where possible. Never merge unrelated matches into one component.

### Phase 2 — Map evidence, not hypotheses

Build an evidence ledger with the fields:

`claim_id | question | observation | source(path:lines/symbol) | evidence_type | certainty | follow_up`

Prioritize these questions:

1. **Purpose and contract:** What triggers the component? What inputs, outputs, invariants, and side effects are observable?
2. **Architecture:** What is its position relative to direct callers and direct dependencies? Where are boundaries and dependency directions verified?
3. **Design:** Which internal abstractions, composition, interfaces, state transitions, decisions, and extensions are actually implemented?
4. **Execution:** What is the real path from entry point to exit, including alternative branches and asynchronous work?
5. **Data:** What gets validated, transformed, stored, published, cached, or returned? Where are transaction boundaries, if any?
6. **Failures and security:** Where are exceptions, cancellation, timeouts, authorization, authentication, secrets, retries, idempotency, and observability handled? Record `not found in inspected paths` rather than claiming global absence.
7. **Wiring and tests:** How is the component instantiated and configured? What relevant tests verify intended behavior, and what do they leave uncovered?

### Phase 3 — Trace and challenge

- Follow the selected component's entry point through relevant calls to effects and return values.
- Examine both static references and runtime wiring (DI registrations, reflection, factories, plugin discovery, config, middleware, routing) when present.
- Inspect the smallest necessary part of collaborating components to verify called contracts.
- Cross-check important claims against tests, configurations, interfaces, call sites, and reachable branches.
- For each high-impact assertion ask: "What exact evidence would make this claim false?" Search for that counterevidence within scope.
- If verification is blocked, downgrade certainty. Never promote an assumption to `CONFIRMED` through repetition.
- Treat checked-in documentation as corroboration, not sufficient proof by itself.

### Phase 4 — Produce the documentation

Use `references/documentation-template.md` as the default structure. Include:

- Identification and scope (IN / CONTEXT / OUT)
- Brief purpose and responsibilities
- Context diagram restricted to the component and immediate collaborators
- Public contracts and dependencies
- Internal design and key abstractions
- Core execution flows and meaningful branches
- Data / state / persistence / integrations relevant to this component
- Error handling, security, resilience, and observability (only what can be verified)
- Configuration and runtime composition
- Tests and verification limits
- Evidence index, unknowns, discrepancies, and follow-up questions

For nontrivial flows, use Mermaid sequence/flow diagrams, with labels based on confirmed code paths. Do not render invented layers, services, or interactions. Omit inapplicable sections instead of padding with generic advice.

### Phase 5 — Quality gate

Before finalizing, verify all checks:

- [ ] Deliverable documents only the selected component; neighbors appear solely as context.
- [ ] Every important behavior/architecture assertion has a repository source.
- [ ] No claims are justified only by names, folder layout, framework expectations, or comments.
- [ ] Runtime wiring and relevant configuration were inspected when needed.
- [ ] Data mutations, external effects, branches, and failure paths were considered.
- [ ] Diagrams faithfully reflect evidence; unknown edges are labeled or omitted.
- [ ] Inferences, contradictions, and unknowns remain explicitly labeled.
- [ ] Missing tests do not become assertions that behavior is absent.
- [ ] No source code or unrelated documentation was modified.
- [ ] Evidence references actually exist and point to the described code.

If any high-impact check fails, qualify the documentation rather than fabricate completeness.

## Efficient investigation guidance

Use `rg --files`, `rg -n`, targeted file reads, language-aware symbol navigation, and bounded reference searches where available. Exclude build outputs, generated code, vendored packages, and secrets by default, but inspect generated code when it is essential to the component. Prefer a small verified dependency graph over a speculative system-wide diagram.

When reading very large files, retrieve relevant line windows and expand on demand. Respect repository-specific agent instructions, but never let them override the evidence, scope, or read-only constraints above.

## Output discipline

- Default artifact: `<component-slug>.architecture.md` if a file was requested; otherwise present the Markdown report directly.
- A summary is not a substitute for an implementer-level explanation of the selected component.
- Never state "the system uses X" when only the scoped component demonstrates X; say "this component uses X".
- End with a compact **Verification status**: `confirmed`, `inferred`, `unverified`, `contradicted` counts and the most consequential blind spots (counts only when an evidence ledger was actually collected).

## Example invocation

> Analyze and document `PaymentRetryWorker` in `src/Billing`, including architecture, retry flow, dependencies, failure handling, and tests. Do not document the entire Billing service.

The target is `PaymentRetryWorker`. Its collaborators may be inspected as context, but the final document must not become a general Billing-service architecture guide.
