# New way of getting code lists

* Status: in progress
* Deciders: Squad Data
* Date: 2025-11-25

## Result
* Not concluded

## Problem context

We want to be able to get code lists through the API without registering the provider in
'Program.cs' as is currently required for the Altinn 2 code lists.

The endpoints we currently have for getting code lists takes an optionId, queryParams and the language as input. Where
the optionId is a random string value configured through Program.cs. We want to be able to get the code lists
without configuring the optionId in Program.cs.

Other things that would be nice to solve at the same time:
* Support for filtering/grouping of code lists
* Clean up APIs
  * Object, not list as root for added metadata support
  * Avoid the need to distinguish between `"secure": true/false` in frontend
* See if we can improve the way we register existing code lists
* Cleanup in service logic in backend (AppOptionsFactory.GetOptionsProvider and InstanceAppOptionsFactory.GetOptionsProvider)

## Decision drivers

* B1: Have object instead of list as root
* B2: Simplify application layout
* B3: Keep complexity low for developers
* B4: Prevent confusion between optionId and "optionId" parsed to org, codelist id and version
* B5: Avoid the need to distinguish between `"secure": true/false` in frontend
* B6: Performance, consider parsing overhead of different approaches

## Alternatives considered

* **A1: Use already existing path without modifying it**
  *GET /{org}/{app}/api/options/{optionsId}?language={language}*
  OptionsId becomes the creator org, codelist id and version.
  Formatting creator org, codelist id and version into the
  optionsId string eg. creatorOrg--codelistId--version
* **A2: Modify existing path with nullable path variables**
  *GET /{org}/{app}/api/options/{optionIdOrCreatorOrg}/
  {codeListId?}/{version?}&language={language}*
  Supports receiving both just optionId or a creatorOrg, codeListId and version combination.
* **A3: Modify existing path with new query parameters**
  *GET /{org}/{app}/api/options/{optionsIdOrCodeListId}?source=library&creatorOrg={org}&version={version}&language={language}*
  optionsIdOrCodeListId becomes the codeListId when source=library
* **A4: Modify existing path so that option id is wild card path segment**
  *GET /{org}/{app}/api/options/{\*\*optionsIdOrLibraryRef}&language={language}*
  OptionId is now allowed to contain slashes,
  and can be formated as /{org}/{codeListId}/{version}
* **A5: Add a new controller method /{creatorOrg}/{codeListId}?version={version}**

## Pros and cons

### A1: Use already existing path without modifying it

* Pros
  * Less work required in the frontend?
* Cons
  * Increased complexity since
  the endpoint now has to encode what is sent
  in as "optionId" to org, codelist id and version.
  * Can potentially cause confusion between what
  is an actual optionId and what is not.
  * String parsing complexity, what should be
  encoded as optionId and what should not be.
  * Difficult to determine a format for optionsId that
  consisting of org, code list id and version
  that doesnt conflict with actual optionsIds
  * If org, code list id and version contains special characters (hyphens, dots, etc), the delimiter choice becomes problematic.
  * Everything is string; framework can't validate individual components.

### A2: Modify existing path with nullable path variables

* Pros
  * Supports B3; no custom parsing of "optionId"
  will help maintain a lower complexity.
  * Supports framework validation, each path segment validated separately by routing.
  * Tools can generate clearer API docs showing both usage patterns
  * RESTful design, clear resource hierarchy in URL path
* Cons
  * Can potentially cause confusion on when certain fields must be provided.
  * Doesnt seem possible to document the optional path parameters out of the box in Swagger, all path parameters are required.
  * The issue above makes it impossible to call the endpoint the old way with just optionsId through Swagger. Swagger complains about missing required parameters missing
  * Route ambiguity, /options/something could match either pattern. So some custom validation will be required.

### A3: Modify existing path with new query parameters

* Pros
  * Clear semantic distinction via source parameter.
  * Supports B3; no custom parsing of "optionId"
    will help maintain a lower complexity.
* Cons
  * Can potentially cause confusion on when certain fields must be provided.
  * REST anti-pattern, resource identifiers (org, codeListId) should be in path, not query string

### A4: Modify existing path so that option id is wild card path segment

* Pros
  * We know that optionsIds never contains slashes. So we can confidently say that
  optionIds containing / is requesting library code lists
* Cons
  * Can potentially cause confusion between what
    is an actual optionId and what is not.
  * String parsing complexity, what should be
    encoded as optionId and what should not be.
  * Route conflicts, wild card can accidentally catch routes you didnt intend.
  * Breaking rest conventions, path parameters should be single identifiers, not composite structures.
  * Poor discoverability, API consumers can't tell from the OpenAPI/Swagger docs what format optionsId should be.

### A5: Add a new controller method /{creatorOrg}/{codeListId}?version={version}

* Pros
  * Easier to document which path parameters that is required in Swagger.
  * It is also easier to document the different responses with two separate endpoints.
* Cons
  * Will require a new endpoint which was something we initially didnt want.

## Decision rationale

