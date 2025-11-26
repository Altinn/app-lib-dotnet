# New endpoint for fetching code lists from Altinn 3 library

* Status: in progress
* Deciders: Squad Data
* Date: 2025-11-25

## Result
* Not concluded

## Problem context

We want to be able to get code lists through
the API without registering the provider in
'Program.cs' as is currently required for
the Altinn 2 code lists.

The endpoints we currently have for getting code lists
takes an optionId, queryParams and the language as input. Where
the optionId is a random string value configured
through Program.cs. We want to be able to get the code lists
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

## Alternatives considered

* **A1: Use already existing path without modifying it**
  *GET /{org}/{app}/api/options/{optionsId}?language={language}*
  OptionsId becomes the org, codelist id and version.
  Formatting org, codelist id and version into the
  optionsId string eg. org--codelistId--version
* **A2: Modify existing path with nullable path variables**
  *GET /{org}/{app}/api/options/{optionIdOrCreatorOrg}/
  {codeListId?}/{version?}&language={language}*
  Supports receiving both just optionId or a creatorOrg, codeListId and version combination.
* **A3: Modify existing path with new query parameters**
  *GET /{org}/{app}/api/options/{optionsIdOrCodeListId}?source=library&creatorOrg={org}&version={version}&language={language}*
  optionsIdOrCodeListId becomes the codeListId when source=library
* **A4: Modify existing path so that option id is wild card path segment**
  *GET /{org}/{app}/api/options/{\*\*optionsId}*
  OptionId is now allowed to contain slashes,
  and can be formated as /{org}/{codeListId}/{version}

* **C1: Old response**
* **C2: New response**

## Pros and cons

### A1: Use already existing path without modifying it

* Pros
* Cons
  * A1C1: Increased complexity since
  the endpoint now has to encode what is sent
  in as "optionId" to org, codelist id and version.
  * A1C2: Can potentially cause confusion between what
  is an actual optionId and what is not.
  * A1C4: String parsing complexity, what should be
  encoded as optionId and what should not be.

### A2: Modify existing path with nullable path variables

* Pros
  * Supports B3; no custom parsing of "optionId"
  will help maintain a lower complexity.
* Cons

### A3: Modify existing path with new query parameters

* Pros
  * Same as for A1
  * Clear semantic distinction via source parameter.
  * No parsing of optionId required.
* Cons

### A4: Modify existing path so that option id is wild card path segment

* Pros
  * Same as for A1.
* Cons
  * A1C2, A1C4.
  * Route conflicts, wild card can accidentally catch routes you didnt intend.
  * Breaking rest conventions, path parameters should be single identifiers, not composite structures.
  * Poor discoverability, API consumers can't tell from the OpenAPI/Swagger docs what format optionsId should be.

### C1: Old response

* Pros
    * Less work required in the frontend?.
    * Less code to maintain if this lets us just use the old endpoint.
    * Supports B2, we can add fields to the response without introducing breaking changes.
    * Supports B5, can be done by allowing both instance- and app options responses from the same endpoint.
* Cons
    * The API response is a list of options instead of an object.

### C2: New response
* Pros
  * Supports B1, B4 and B5.
  * Returning object instead of list adds support for returning metadata in a better way then we do now with a header field.
  * Supports B2; we can create a new field in layout and move the existing code list fields to it as well as adding new ones.
* Cons
  * Increased cost in development time in both frontend and backend.
  * Will add more code to maintain as we will have to keep the old endpoint around at least for a while.
