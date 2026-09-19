# AGENTS.md

## Purpose

This directory contains feature specifications.

The goal of a feature specification is to define **what must be built and why**, before implementation begins.

Specifications must reduce ambiguity enough that another engineer or agent can implement the feature without having to reconstruct product intent from scattered context.

A specification is not implementation code.

It may describe technical constraints when they are relevant to behavior, integration, architecture, security, performance, compatibility, or operational requirements.

---

## Core Principle

Separate:

* problem;
* expected behavior;
* constraints;
* decisions;
* implementation.

A specification should define behavior and constraints first.

Implementation details should only be included when they are part of an explicit architectural decision or necessary to prevent incompatible implementations.

---

## Specification Workflow

When creating a feature specification, follow this order:

```text
Context
↓
Problem
↓
Goals
↓
Non-Goals
↓
Requirements
↓
Behavior
↓
Rules and Invariants
↓
Edge Cases
↓
Technical Constraints
↓
Acceptance Criteria
↓
Open Questions
↓
Implementation
```

Do not jump directly from a feature request to code.

---

# Feature Specification Structure

Every feature specification should follow the structure below.

---

## 1. Feature

Provide a short and explicit name.

Example:

```markdown
# Feature: Product Search
```

Avoid vague names such as:

```text
Search improvements
Changes
Product update
New functionality
```

The title should describe the capability being introduced.

---

## 2. Summary

Describe the feature in a few sentences.

Answer:

* what capability is being introduced;
* who uses it;
* what problem it solves.

Example:

```markdown
## Summary

Allow users to search products by name from the product listing page.

The feature reduces the need to manually navigate large product lists and allows users to quickly locate a specific product.
```

Keep this section concise.

---

## 3. Context

Describe the relevant current behavior.

Include only information necessary to understand the feature.

Examples:

* current workflow;
* existing limitation;
* relevant system behavior;
* existing architectural constraints.

Example:

```markdown
## Context

The product listing currently returns all active products.

Users must manually browse the list to find a specific product.

As the number of products grows, this becomes inefficient.
```

Do not turn the context section into general project documentation.

---

## 4. Problem

Clearly describe the problem being solved.

Focus on the problem, not the proposed implementation.

Prefer:

```markdown
Users with large product catalogs cannot efficiently locate a specific product.
```

Avoid:

```markdown
We need to add a search box and LINQ Where clause.
```

The second statement already assumes an implementation.

---

## 5. Goals

Describe the expected outcomes.

Goals must be observable.

Example:

```markdown
## Goals

- Allow users to search products by name.
- Preserve existing product listing behavior when no search term is provided.
- Allow users to clear the search and return to the complete list.
```

Avoid goals such as:

```text
Improve the code.
Make search better.
Create a scalable solution.
```

Unless they are accompanied by measurable requirements.

---

## 6. Non-Goals

Explicitly describe what is outside the feature scope.

Example:

```markdown
## Non-Goals

This feature does not include:

- fuzzy search;
- search by product description;
- full-text search infrastructure;
- autocomplete;
- search history;
- advanced filtering.
```

Non-goals are important because they prevent scope expansion during implementation.

---

## 7. Actors

Identify the actors involved when relevant.

Example:

```markdown
## Actors

### User

Can search and browse products.

### System

Receives the search term and returns matching products.
```

Actors may include:

* authenticated user;
* administrator;
* external system;
* scheduled process;
* API client;
* background worker;
* integration provider.

Skip this section when the feature has no meaningful actor distinction.

---

## 8. Preconditions

Describe conditions that must already be true before the feature behavior occurs.

Example:

```markdown
## Preconditions

- The user can access the product listing page.
- Products already exist in the system.
```

Do not list obvious infrastructure assumptions unless they affect behavior.

---

# Requirements

Requirements define mandatory behavior.

Requirements must be:

* explicit;
* independently understandable;
* testable;
* implementation-neutral whenever possible.

Use stable identifiers.

Example:

```markdown
## Functional Requirements

### FR-001 — Search by product name

The system must allow the user to provide a search term when viewing the product list.

### FR-002 — Filter matching products

When a search term is provided, the system must return products whose names match the search criteria.

### FR-003 — Empty search

When the search term is empty or absent, the system must return the normal product listing.

### FR-004 — Preserve search term

The search term must remain visible after the search is executed.
```

Requirement IDs must not be reused after removal.

---

## Requirement Language

Use normative language consistently.

Use:

* **must** — mandatory;
* **must not** — prohibited;
* **should** — recommended, but not mandatory;
* **may** — optional behavior.

Avoid ambiguous terms such as:

* preferably;
* usually;
* ideally;
* whenever possible;
* fast;
* simple;
* intuitive.

If such terms are necessary, define what they mean.

---

# User Flow

Describe the expected user or system flow.

Example:

```markdown
## User Flow

1. User opens the product listing page.
2. System displays the current product list.
3. User enters a product name.
4. User submits the search.
5. System filters the available products.
6. System renders the filtered result.
7. User may clear the search.
8. System returns to the complete list.
```

Focus on externally observable behavior.

Do not describe method calls or class interactions here.

---

# Scenarios

Use scenarios to describe concrete behavior.

Prefer Given / When / Then.

Example:

```markdown
## Scenarios

### Scenario: Search returns matching products

Given products "Notebook", "Monitor", and "Mouse" exist
And the user is on the product listing page
When the user searches for "Monitor"
Then the result must include "Monitor"
And the result must not include "Notebook"
And the result must not include "Mouse"
```

---

## Scenario: Empty search

```markdown
Given active products exist
When the user submits an empty search
Then the system must display the normal product listing
```

Scenarios should demonstrate important behavior, not repeat every requirement mechanically.

---

# Business Rules

Business rules represent domain decisions.

Give each relevant rule a stable identifier.

Example:

```markdown
## Business Rules

### BR-001 — Search only active products

Search results must contain only products that are eligible for the normal product listing.

### BR-002 — Case-insensitive matching

Product name matching must not depend on character casing.
```

A business rule should remain valid regardless of UI or technical implementation.

---

# Invariants

An invariant defines something that must always remain true.

Example:

```markdown
## Invariants

- Search must never expose products the user cannot normally access.
- Applying a search must not modify product data.
- Search behavior must preserve the same authorization rules as the normal product listing.
```

Invariants are especially useful for:

* permissions;
* workflows;
* state machines;
* financial rules;
* distributed processes;
* consistency guarantees.

---

# Data

Describe data involved only when relevant to expected behavior.

Example:

```markdown
## Data

### Input

Search term:

- Type: string
- Optional: yes
- Maximum length: 100 characters

### Output

A collection of products the current user is authorized to view and that match the search criteria.
```

Do not prematurely define database schemas unless required.

---

# Validation

Describe input validation explicitly.

Example:

```markdown
## Validation

- A missing search term is valid.
- Whitespace-only search terms must behave as an empty search.
- Search terms longer than 100 characters must be rejected.
```

Specify the expected system behavior for invalid input.

Do not simply state:

```text
Validate the search field.
```

---

# Error Handling

Describe expected failure behavior.

Example:

```markdown
## Error Handling

If the product list cannot be loaded:

- the system must not display partial or misleading results;
- the failure must follow the application's existing error handling behavior;
- technical exception details must not be exposed to the user.
```

Only define error behavior relevant to the feature.

---

# Authorization

Explicitly describe authorization rules when applicable.

Example:

```markdown
## Authorization

Search must follow the same authorization rules as the normal product listing.

A user must never discover a product through search if that product would otherwise be inaccessible to that user.
```

Security rules must not depend solely on UI behavior.

---

# Security

Consider whether the feature introduces risks related to:

* authorization;
* authentication;
* data exposure;
* injection;
* XSS;
* CSRF;
* file access;
* external input;
* secrets;
* sensitive information.

Example:

```markdown
## Security

The search term is untrusted user input.

The implementation must not construct database queries through string concatenation.
```

Do not prescribe a specific library unless necessary.

---

# Performance

Add performance constraints only when relevant.

Prefer measurable limits.

Example:

```markdown
## Performance Requirements

For catalogs containing up to 100,000 products, the search endpoint should return within the application's existing response-time target under normal operating conditions.
```

Avoid:

```text
Search must be fast.
```

If there is no real performance requirement, omit the section.

---

# Observability

Define required operational visibility when relevant.

Example:

```markdown
## Observability

Unexpected failures during product search must be captured by the application's existing logging and monitoring infrastructure.

The feature must not create a log entry for every successful search unless required by existing application conventions.
```

For business-sensitive workflows, relevant events may also be specified.

---

# Compatibility

Describe compatibility requirements.

Example:

```markdown
## Compatibility

The existing `/Products` route must continue working without requiring a search term.

Existing links to the product listing must remain valid.
```

Consider:

* URLs;
* APIs;
* persisted data;
* events;
* database schema;
* configuration;
* integrations.

---

# Edge Cases

List important edge cases explicitly.

Example:

```markdown
## Edge Cases

- Search term is null.
- Search term is empty.
- Search term contains only whitespace.
- No products match.
- One product matches.
- Many products match.
- Search term differs only in case.
- User attempts to search for an inaccessible product.
```

Do not include impossible or irrelevant cases just to increase coverage.

---

# Dependencies

Identify relevant dependencies.

Example:

```markdown
## Dependencies

This feature depends on:

- the existing product listing;
- the current product authorization rules;
- the existing product persistence mechanism.
```

Do not create speculative dependencies.

---

# Constraints

Describe known restrictions.

Example:

```markdown
## Constraints

- The feature must use the current ASP.NET Core MVC architecture.
- The existing product listing route must remain compatible.
- No external search infrastructure will be introduced.
```

Constraints should describe boundaries, not arbitrary implementation preferences.

---

# Architecture Impact

Describe the expected impact on the system at a conceptual level.

Example:

```markdown
## Architecture Impact

The feature affects:

- product listing request handling;
- product query behavior;
- product listing View Model;
- product listing Razor View.

No new external infrastructure is expected.
```

Do not define classes that do not yet need to exist.

---

# Technical Decisions

Technical decisions belong here only when they have already been intentionally chosen.

Example:

```markdown
## Technical Decisions

### TD-001 — Search remains server-side

Product search will execute on the server rather than filtering the already-rendered HTML on the client.

Reason:

The complete product dataset may not be loaded into the browser.
```

Every significant technical decision should contain its rationale.

---

# Alternatives Considered

For non-trivial features, document meaningful alternatives.

Example:

```markdown
## Alternatives Considered

### Client-side filtering

Rejected because the browser does not necessarily contain the complete product dataset.

### External full-text search

Not selected because the current feature requires only simple name matching and does not justify additional infrastructure.
```

Do not invent alternatives for trivial decisions.

---

# Acceptance Criteria

Acceptance criteria define when the feature can be considered complete.

They must be testable.

Example:

```markdown
## Acceptance Criteria

- A user can search products by name.
- Matching products are displayed.
- Non-matching products are excluded.
- Empty search returns the normal product list.
- Search remains subject to existing authorization rules.
- Existing product listing URLs continue to work.
- Relevant automated tests pass.
```

Acceptance criteria should describe outcomes, not implementation steps.

Avoid:

```text
Create ProductSearchService.
Add method to repository.
Add search button.
```

Those are implementation tasks.

---

# Test Considerations

Describe important categories of verification.

Example:

```markdown
## Test Considerations

The implementation should verify at minimum:

### Happy Path

Searching for an existing product returns it.

### No Results

Searching for an unknown product returns an empty result.

### Empty Input

Empty search behaves as the normal listing.

### Authorization

Products unavailable to the current user remain unavailable through search.

### Case Handling

Search behavior respects the defined casing rule.
```

Do not attempt to fully design the test suite inside the specification unless required.

---

# Out of Scope

Use this section to reinforce important scope boundaries.

Example:

```markdown
## Out of Scope

The following are explicitly excluded:

- search suggestions;
- Elasticsearch;
- typo correction;
- relevance ranking;
- search analytics;
- advanced filters.
```

---

# Open Questions

Questions without a resolved decision must remain explicit.

Example:

```markdown
## Open Questions

- Should partial matching be supported?
- Should the search include product codes?
- Is there a maximum result count?
```

Do not silently invent answers.

An unresolved question that affects observable behavior should normally be resolved before implementation.

---

# Risks

For non-trivial features, identify meaningful risks.

Example:

```markdown
## Risks

### Query performance

Partial string matching may become expensive as the product catalog grows.

Mitigation:

Keep the query compatible with the current persistence strategy and validate query performance against realistic data volume.
```

Avoid generic risks that apply to every software change.

---

# Implementation Notes

This section is optional.

It may contain guidance that helps implementation without turning the specification into a coding plan.

Example:

```markdown
## Implementation Notes

The existing product listing query should be reused where possible so search results automatically inherit existing product visibility rules.
```

Implementation notes are guidance, not requirements, unless explicitly referenced by a requirement.

---

# Requirement Traceability

For larger features, requirements should be traceable to scenarios and acceptance criteria.

Example:

```text
FR-001
 ├── Scenario: Search returns matching products
 └── AC-001

FR-003
 ├── Scenario: Empty search
 └── AC-003
```

Traceability is useful when specifications become large or evolve over time.

---

# Specification Quality Rules

Before considering a specification complete, verify that:

* the problem is clearly stated;
* goals describe outcomes;
* non-goals define scope boundaries;
* requirements are testable;
* requirements do not unnecessarily prescribe implementation;
* business rules are explicit;
* edge cases are identified;
* authorization implications are considered;
* error behavior is defined where necessary;
* acceptance criteria map to requirements;
* unresolved decisions appear under Open Questions;
* the specification does not contradict itself.

---

# Avoid Implementation Leakage

Specifications should avoid implementation details such as:

```text
Create ProductSearchService.

Add SearchAsync to IProductRepository.

Use LINQ Contains.

Create SearchProductRequest.cs.

Add a GET action to ProductsController.
```

Unless those decisions are already part of an architectural requirement.

Prefer:

```text
The system must support filtering the product listing by product name.
```

Implementation design belongs to a later phase.

---

# Avoid Ambiguous Requirements

Bad:

```text
The system should support product search.
```

Better:

```text
When a user provides a non-empty search term, the product listing must contain only products whose names match the configured search behavior.
```

Bad:

```text
The page should load quickly.
```

Better:

```text
The feature must not require loading the complete product catalog into the browser.
```

---

# Specification vs Implementation Plan

Do not mix these artifacts.

A specification answers:

```text
What must the system do?
Why does it need to do it?
What rules must remain true?
What defines success?
```

An implementation plan answers:

```text
Which components will change?
Which classes will be created?
Which methods will be modified?
In what order will the work be performed?
```

The specification should be understandable without the implementation plan.

---

# Agent Behavior

When asked to create a feature specification:

1. inspect available project context;
2. identify existing relevant behavior;
3. extract explicit requirements from the request;
4. distinguish requirements from assumptions;
5. avoid silently converting assumptions into requirements;
6. identify relevant edge cases;
7. identify authorization and security implications;
8. identify compatibility concerns;
9. identify unresolved behavior;
10. produce the specification before proposing implementation.

Do not write production code while the specification is still being defined unless explicitly requested.

---

# Handling Missing Information

When information is missing but does not materially affect behavior, document a reasonable assumption.

Example:

```markdown
## Assumptions

- Search applies only to products already visible in the normal listing.
```

When missing information materially changes externally observable behavior, add it to:

```markdown
## Open Questions
```

Do not invent product decisions.

---

# Specification Evolution

Specifications may evolve.

When changing an existing specification:

* preserve stable requirement IDs;
* add new requirement IDs rather than renumbering existing ones;
* explicitly modify outdated requirements;
* remove contradictions;
* keep acceptance criteria synchronized;
* update scenarios affected by the change.

Do not leave obsolete requirements active alongside their replacements.

---

# Recommended Specification File

Each feature should normally have its own specification.

Example:

```text
/specs
    /product-search
        spec.md
```

For more complex features:

```text
/specs
    /product-search
        spec.md
        decisions.md
        implementation-plan.md
```

Keep the behavioral specification separate from implementation planning whenever possible.

---

# Definition of Ready

A specification is ready for implementation when:

* the problem is understood;
* scope is explicit;
* mandatory behavior is defined;
* important edge cases are covered;
* business rules are defined;
* authorization implications are defined;
* compatibility requirements are known;
* acceptance criteria are testable;
* no unresolved question blocks implementation.

A specification does not need to define every implementation detail before work begins.

It must define enough behavior that implementation choices cannot accidentally change the intended product behavior.

---

# Final Principle

The specification is the contract between intent and implementation.

Code may change.

Architecture may evolve.

Implementation details may be replaced.

The required behavior, constraints, and invariants defined by the specification remain the source of truth for the feature.
