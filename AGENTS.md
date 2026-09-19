# Feature Development Workflow

Feature development in this repository follows a specification-driven workflow.

The standard flow is:

```text
Feature Request
↓
Specification
↓
Planning
↓
Implementation
↓
Review
```

Each stage has a specific responsibility and should remain separated from the others.

The corresponding agents are located under:

```text
.agents/
├── specification.md
├── planning.md
├── implementation.md
└── review.md
```

---

## 1. Specification

Use:

```text
.agents/specification.md
```

The specification phase defines the expected system behavior before implementation decisions are made.

It produces a feature specification such as:

```text
specs/<feature>/spec.md
```

The specification defines:

* the problem;
* feature goals;
* non-goals;
* functional requirements;
* business rules;
* invariants;
* user and system flows;
* edge cases;
* security requirements;
* compatibility constraints;
* acceptance criteria.

The specification answers:

```text
WHAT must the system do?
```

Do not start implementation while material behavioral questions remain unresolved.

---

## 2. Planning

Use:

```text
.agents/planning.md
```

The planning phase analyzes the approved specification against the actual repository.

It produces:

```text
specs/<feature>/plan.md
```

The planner must inspect the existing codebase before defining the solution.

The plan identifies:

* affected components;
* existing components to reuse;
* files to modify;
* files to create;
* persistence changes;
* API changes;
* UI changes;
* configuration changes;
* security implications;
* test strategy;
* implementation tasks;
* task dependencies;
* expected implementation order.

The planning phase answers:

```text
HOW should the existing system be changed?
```

Do not use the planning phase to redefine product requirements.

---

## 3. Implementation

Use:

```text
.agents/implementation.md
```

The implementation phase consumes both:

```text
spec.md
plan.md
```

and produces the actual code changes.

Implementation must:

* follow the specification;
* follow the implementation plan where technically valid;
* preserve existing architecture and conventions;
* keep changes scoped to the feature;
* add or update relevant tests;
* preserve backward compatibility unless explicitly changed;
* validate the build;
* execute relevant tests;
* document meaningful deviations from the plan.

The implementation phase answers:

```text
MAKE the system satisfy the specification.
```

The implementation agent must not introduce new product behavior that is absent from the specification.

---

## 4. Review

Use:

```text
.agents/review.md
```

The review phase validates the completed implementation against:

```text
spec.md
plan.md
actual code changes
tests
```

The review should verify:

* requirement coverage;
* acceptance criteria;
* business rules;
* invariants;
* plan adherence;
* architecture consistency;
* backward compatibility;
* security;
* performance regressions;
* test coverage;
* unnecessary scope expansion.

The review phase answers:

```text
DID we implement the correct feature correctly?
```

Review findings should be tied to concrete requirements, implementation decisions, or repository conventions.

---

# Workflow Rules

For non-trivial feature work, do not skip directly from a feature request to implementation.

Prefer:

```text
Request
↓
Spec
↓
Plan
↓
Code
↓
Review
```

For trivial changes such as:

* typo corrections;
* simple content changes;
* obvious styling adjustments;
* isolated low-risk fixes;

the full workflow may be unnecessary.

Use engineering judgment proportional to the complexity and risk of the change.

---

# Artifact Structure

Recommended structure:

```text
/
├── .agents/
│   ├── specification.md
│   ├── planning.md
│   ├── implementation.md
│   └── review.md
│
├── specs/
│   └── <feature-name>/
│       ├── spec.md
│       └── plan.md
│
└── ...
```

For larger features, additional artifacts may be added:

```text
specs/<feature-name>/
├── spec.md
├── plan.md
└── decisions.md
```

Do not create additional artifacts unless they provide clear value.

---

# Source of Truth

For feature development, use the following hierarchy:

```text
Specification
↓
Implementation Plan
↓
Implementation
```

The specification is the source of truth for behavior.

The plan is the source of truth for the intended technical approach.

The implementation is the concrete realization of both.

If the plan conflicts with the specification:

```text
Specification wins.
```

If the plan conflicts with the actual repository structure but the specification can still be preserved:

```text
Adapt the implementation minimally
and document the deviation.
```

---

# Feature Completion

A feature is not considered complete merely because code was written.

A feature is complete when:

* the specification requirements are implemented;
* acceptance criteria are satisfied;
* the implementation plan is completed or deviations are documented;
* the project builds;
* relevant tests pass;
* security and compatibility constraints are preserved;
* review does not identify blocking issues.

---

# Guiding Principle

Keep the responsibilities explicit:

```text
Specification → define behavior
Planning       → design the change
Implementation → execute the change
Review          → verify the result
```

Avoid collapsing all four phases into a single step for non-trivial feature work.
