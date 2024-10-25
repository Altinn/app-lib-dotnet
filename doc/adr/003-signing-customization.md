# How should we support customization for singing?

-   Status: Proposed
-   Deciders: Team
-   Date: Proposal date

## Result

A3 - Full freedom to compose smaller components into the "signing component" as wanted - with a default configuration provided by studio.

## Problem context

There are many use cases we are able to support for signing without requiring a layout-set.
All other tasks have a layout set. For payment, the layout set was perhaps not needed - we are able to support relevant customer needs because of this.

## Decision drivers

-   B1: App developers are used to being able to customize the layouts.
-   B2: Altinn Studio as a product should have offer a default "view" for the signing task.
-   B3: We should adhere to established standards in our frontend code base.
-   B4: Should be simple/intuitive to maintain the code base.
-   B5: We do not know all possible use cases for service owners.
-   B6: Pit of success should be wide and deep - it should be as hard as possible to configure an app into an unstable / not working state. Should be difficult to develop bad UX
-   B7: Backwards compability

## Alternatives considered

-   A1: Use a layout set with a signing component. The signing component itself has limited options for customization, but everything "around" in configurable.
-   A2: Do not use a layout set - all logic/config is known from and based on the task type
-   A3: Full freedom to compose smaller components into the "signing component" as wanted - with a default configuration provided by studio.
    -   this includes:
        -   yes/no have a list of signees
        -   yes/no have a list of documents
        -   yes/no use the panel for state

## Pros and cons

List the pros and cons with the alternatives. This should be in regards to the decision drivers.

### A1

-   Good, B2, B3, B4 (as intuitive as it is today), B7
-   Neutral, because B1 and B6 is only partially covered.
-   Bad, because it does not fullfill B5

### A2

-   Good, because this alternative adheres to B2, B4,
    -   B6: impossible to not fall in the pit of success - UNLESS you have non-standard needs
-   Nautral, because B7 requires more effort on the Team Apps develpers
-   Bad, B3 by using the "based on the type of task we are in" standard - but not the "use layout set and ignore task type" standard.
-   Bad, because it does not fullfill the B1, B5 decision driver.

### A3

-   Good, because this alternative adheres to B1, B2, B3, B5, B7
-   Neutral, B6: More possibilities - more thing can go wrong. More possibilities - more custom for a specific need (better ux).
-   Bad, because it does not fullfill B4, B6
    -   More external components - more support/maintainance
