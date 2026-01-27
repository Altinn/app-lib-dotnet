# Moving to a Monorepo

- Status: Proposed
- Deciders: Team
- Date: 15.11.2024

## Result

This ADR builds on the broader SDLC ADR: https://github.com/Altinn/app-lib-dotnet/pull/860

It narrows down to focus on monorepo so that can be improved and implemented in isolation.

## Problem Context

The separation of frontend and backend repositories creates challenges specific to collaboration, tooling, and testing:

-   Frontend and backend development work is disjoint
    -   Leads to less cohesive code, worse APIs
    -   Slower development time due to handover
    -   Worse testing (parts of the stack are tested in isolation, as opposed to the whole of a feature/fix)
    -   Team members have expressed desire to work full-stack
    -   Repo separation inhibits tooling and automation across the full stack (e.g. integration tests across frontend and backend within a PR/feature branch)
    -   Apps used for e2e testing should be part of the monorepo to be able to see the full extent of the PR

## Decision Drivers

- **M01**: Enable cohesive development of frontend and backend within a single repository by using shared tooling like a common CI/CD pipeline, enforcing unified coding standards, and leveraging a consistent folder structure to facilitate collaboration and maintainability.
- **M02**: Simplify tooling and dependency management across the stack
- **M03**: Support full-stack testing and deployment workflows in a unified pipeline.
- **M05**: Reduce the overhead of managing separate repositories.
- **M06**: Facilitate fullstack development
- **M07**: Features should be developed cohesively - considering the full stack of changes needed at a time, to avoid inefficiencies and poor design
- **B09**: Releases should not lead to inconsistencies/errors due to differing versions (i.e. mismatch of static assets on frontend)
- **B10**: We should be able to test a whole feature during development in a single PR both locally and in tt02

## Alternatives Considered

### Refactor into a Monorepo

- Consolidate frontend and backend codebases into a single repository.
- Align versioning between frontend and backend for consistent releases.
- Enable PR-based full-stack testing and deployment.
- Facilitate dependency sharing and type generation across the stack.
- Simplify infrastructure for building and serving frontend and backend together.

## Pros and Cons

### Pros

- Unified development and testing workflows, enabling cohesive full-stack changes.
- Improved collaboration and transparency for the entire team.
- Simplified dependency management and potential for code reuse.
- Reduced overhead for developers managing multiple repositories.
- Unified infrastructure and tooling support across the stack.

### Cons

- Increased complexity in initial project setup and build processes.
  - Can be mitigated with good tooling and documentation.
- Risk of larger PRs containing frontend and backend changes, potentially complicating reviews.
  - Can be mitigated by having multiple people review if necessary, keep PRs specific.
- Higher potential for repository-wide issues affecting multiple areas.
- 1 repo with issues related to both frontend and backend
  - Can be mitigated by labelling issues for example

## Conclusion

Moving to a monorepo directly addresses the inefficiencies and challenges created by the separation of repositories. By unifying the codebase, the team can achieve faster development cycles, improved collaboration, streamlined testing, and a more efficient release process, ultimately leading to a more cohesive and productive workflow. By unifying the frontend and backend into a single repository, the team can improve collaboration, streamline workflows, and achieve a more consistent and efficient development process. While initial setup is more complex, the long-term benefits justify the change.
