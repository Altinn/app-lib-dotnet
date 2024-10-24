# A short descriptive title - what is the outcome of this adr?

-   Status: Proposed
-   Deciders: Team
-   Date: 01.11.2024

## Result

Which alternative was chosen. Do not include any reasoning here.

## Problem context

We want to improve several aspects of SDLC for team Apps.

Development and testing:
* Low confidence in the state of `main` leads to lower release cadence
  * PRs in frontend aren't blocked from merging if e.g. some tests fail, partially due to flake tests (Cypress)
  * PRs are sometimes merged before they are manually tested
    * Partially due to the existing process
    * Partially due to missing infrastructure for testing unmerged code in e.g. tt02
* Frontend and backend development work is disjoint
  * Leads to less cohesive code, worse APIs
  * Slower development time due to handover
  * Worse testing (parts of the stack are tested in isolation, as opposed to the whole of a feature/fix)
  * Team members have expressed desire to work full-stack
  * Repo separation inhibits tooling and automation across the full stack (e.g. integration tests across frontend and backend within a PR/feature branch)
* Code review process is sometimes slow
  * Time spent in review is not tracked - both with respect to the PR/work item itself and time spent by devs reviewing
  * No one is "allocated" to reviews
  * Different devs have different habits
* No dedicated tester
  * Developers test their own work (or reviewers in the same "task force")
  * Dedicated testers bring a better process and competence
* Frontend cycle time is slow
  * There are a lot of Cypress tests that take a long time
  * UI components are coupled to the app context, so they can't be tested in isolation (what _is_ the cost of the app context test harness)


Release:
* Versioning
  * Conceptually we support development and ownership of Altinn Apps, yet TEs have to relate to 2 different sets of versions (frontend and backend)
  * We often go straight from preview to final releases
    * More TEs are exposed to more bugs
    * Less time for us to adjust and rethink interfaces and their usage
* Releases are manually published
  * Sometimes there are operator errors
  * Naturally leads to lower release cadence
  * DORA metrics are incoming, at which point our cadence will be measured and tracked by management
* CDN deployed assets of frontend can lead to clientside inconsistency (NOTE: unsure if this is appropriate to address in this ADR, does not relate to SDLC)


## Decision drivers

A list of decision drivers. These are points which can differ in importance. If a point is "nice to have" rather than
"need to have", then prefix the description.

Examples

-   B1: The solution make it easier for app developers to develop an app.
-   B2: Nice to have: The solution should be simple to implement for out team.

## Alternatives considered

List the alternatives that were considered as a solution to the problem context.

-   A1: A solution to the problem.
-   A2: Another solution to the problem

## Pros and cons

List the pros and cons with the alternatives. This should be in regards to the decision drivers.

### A1

-   Good, because this alternative adheres to B1.
    -   Optional explanation as to how.
-   Bad, because it does not fullfill the B2 decision driver.

### A2

-   Good, because this alternative adheres to B2.
-   Bad, because it does not fullfill the B1 decision driver.
