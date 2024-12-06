# A short descriptive title - what is the outcome of this adr?

-   Status: Proposed
-   Deciders: Team
-   Date: 15.11.2024

## Result

Which alternative was chosen. Do not include any reasoning here.

## Problem context

We want to improve several aspects of SDLC for team Apps.

Development and testing:

-   Low confidence in the state of `main` leads to lower release cadence
    -   PRs in frontend aren't blocked from merging if e.g. some tests fail, partially due to flake tests (Cypress)
    -   PRs are sometimes merged before they are manually tested
        -   Partially due to the existing process
        -   Partially due to missing infrastructure for testing unmerged code in e.g. tt02
-   Frontend and backend development work is disjoint
    -   Leads to less cohesive code, worse APIs
    -   Slower development time due to handover
    -   Worse testing (parts of the stack are tested in isolation, as opposed to the whole of a feature/fix)
    -   Team members have expressed desire to work full-stack
    -   Repo separation inhibits tooling and automation across the full stack (e.g. integration tests across frontend and backend within a PR/feature branch)
-   Code review process is sometimes slow
    -   Time spent in review is not tracked - both with respect to the PR/work item itself and time spent by devs reviewing
    -   No one is "allocated" to reviews
    -   Different devs have different habits
-   No dedicated tester
    -   Developers test their own work (or reviewers in the same "task force")
    -   Dedicated testers bring a better process and competence
-   Frontend cycle time is slow
    -   There are a lot of Cypress tests that take a long time
    -   UI components are coupled to the app context, so they can't be tested in isolation (what _is_ the cost of the app context test harness)

Release:

-   Versioning
    -   Conceptually we support development and ownership of Altinn Apps, yet TEs have to relate to 2 different sets of versions (frontend and backend)
    -   We often go straight from preview to final releases
        -   More TEs are exposed to more bugs
        -   Less time for us to adjust and rethink interfaces and their usage
-   Releases are manually published
    -   Sometimes there are operator errors
    -   Naturally leads to lower release cadence
    -   DORA metrics are incoming, at which point our cadence will be measured and tracked by management
-   CDN deployed assets of frontend can lead to clientside inconsistency (NOTE: unsure if this is appropriate to address in this ADR, does not relate to SDLC)

## Decision drivers

-   B01: We should have high confidence in the code in main
-   B02: The state in main should be releasable at all times
-   B03: Releases should be automatic
-   B04: We should be able to make hotfixes and patch releases to different major/minor versions (e.g. cherrypicking)
-   B05: We should measure release and be aware of release cadence over time
-   B06: We should set aside time for new features (minor version bumps) to stabilize in a RC phase in addition to previews, before moving on to stable versions
-   B07: Automatic tests to achieve high confidence should not be so slow as to be an obstacle for quick hotfix
-   B08: We should be able to rollback a release if needed
-   B09: Releases should not lead to inconsistencies/errors due to differing versions (i.e. mismatch of static assets on frontend)
-   B10: Features should be developed cohesively - considering the full stack of changes needed at a time, to avoid inefficiencies and poor design
-   B11: Versioning should be simple to understand, i.e. which versions fit together
-   B12: We should be able to test a whole feature during development in a single PR both locally and in tt02
-   B13: We should have quick and efficient development cycle and feedback loops

## Alternatives considered

List the alternatives that were considered as a solution to the problem context.

-   A1:
    -   Refactor into a monorepo (B01, B09, B10, B11?, B12, B13)
    -   Consolidate major.minor version numbers, making them equal between frontend and backend (B11)
    -   PR builds of frontend and backend should be deployed to CDN and [Github Packages NuGet registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry) (B01, B02, B10, B12, B13)
    -   Implement [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) to automate release branches and versions using GitHub Actions with path filters (B02, B03, B04, B06, B08?, B11, B12)
    -   Tag releases with environments (specifically `production`) (B05)
    -   Write pure and decoupled code to improve testability and simplify writing unittests (less mocking, more plain input/output) (B07, B13)
    -   Cover code and components with unittests, while integration tests cover important workflows/user flows (B07, B13)
    -   GitHub Actions workflows to unpublish from CDN and obsolete(?) from NuGet (B08)
    -   Serve frontend from backend, to ensure consistency between artifacts ?????? (B09)

## Pros and cons

List the pros and cons with the alternatives. This should be in regards to the decision drivers.

### A1

-   Good, it solves all our problems (????)
-   Bad, it's a lot of work and complicated

### Monorepo

#### PROS:

-   Frontend og backend endringer i samme PR
-   Enklere for teamet å følge endringer fullstack, høyere transparency
-   Enklere å jobbe fullstack
-   Kraftige muligheter som generering av typer, splitte ting i libs, gjenbruke kode
-   Slipper å ha flere editorer åpen for flere prosjekter
-   Slipper å committe i flere repoer
-   Kun en clone
-   Dele og optimalisere dependencies
-   Felles tester hvis man får testappene

#### CONS:

-   Frontend og backend endringer i samme PR
-   Mer komplekst oppsett av prosjektet, bygg mm
-   Flere issues i samme repo
