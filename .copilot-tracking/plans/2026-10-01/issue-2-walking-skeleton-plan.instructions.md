---
applyTo: '.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Issue 2 Walking Skeleton

## Overview

Build the first end-to-end .NET 8 slice that uploads one local file to a private
Azurite container through a tested storage seam and reports a sanitized result.

## Objectives

### User Requirements

* Create an implementation plan for GitHub issue #2, "Build the walking
  skeleton: upload a file to a container." Source: User request and
  <https://github.com/Azure-Samples/hve-workshop-demo/issues/2>.
* Plan the solution in the context of the full repository. Source: User request
  and repository `README.md`.

### Derived Objectives

* Preserve issue #1's unresolved collision decision by using non-overwriting
  SDK behavior without adding a public collision outcome. Derived from: issue
  #2's empty-container assumption and rename history.
* Keep the default build and test loop independent of Azure, credentials,
  network egress, and a running emulator. Derived from: README workshop goals
  and issue #2 testing criteria.
* Produce truthful clone-to-upload documentation and keep Azurite persistence
  out of source control. Derived from: issue #2's done condition and documented
  `.azurite` location.

## Context Summary

### Project Files

* `README.md` - Defines the .NET 8/Azurite workshop, target command, planned
  layout, and test split.
* `.gitignore` - Visual Studio baseline that needs one `.azurite/` exclusion.
* `.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md`
  - Canonical issue-specific findings and selected direction.
* `.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md`
  - Complete issue record, parent constraints, SDK evidence, and file design.

### References

* <https://github.com/Azure-Samples/hve-workshop-demo/issues/2> - Task scope,
  tests, and done condition
* <https://github.com/Azure-Samples/hve-workshop-demo/issues/1> - Parent target
  architecture, package pins, and deferred decisions
* <https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite> -
  Local emulator behavior and persistence
* <https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient.uploadasync> -
  Blob stream upload behavior

### Standards References

* C# instructions - Project structure, nullable code, naming, cancellation, and
  public XML documentation
* C# test instructions - xUnit behavior naming, Arrange/Act/Assert, and cleanup
* Markdown and writing-style instructions - Documentation structure and prose

## Architecture Overview

```text
Program.cs
  -> configuration + argument parsing + exit/output mapping
  -> UploadService
       -> local source validation + result mapping
       -> IBlobUploader
            -> AzureBlobUploader -> BlobContainerClient -> Azurite

UploadServiceTests -> FakeBlobUploader
AzuriteIntegrationTests -> AzureBlobUploader -> Azurite
```

Expected source tree:

```text
SchemeFileUploader.sln
Directory.Build.props
src/
  SchemeFileUploader/
  SchemeFileUploader.Cli/
tests/
  SchemeFileUploader.Tests/
data/
  contributions-2026-07.csv
```

## Implementation Checklist

### [x] Implementation Phase 1: Scaffold the Solution

<!-- parallelizable: false -->

* [x] Step 1.1: Create the solution and shared .NET 8 build settings.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 18-42)
* [x] Step 1.2: Create the library, CLI, and xUnit projects with references and
  pinned packages.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 43-77)
* [x] Step 1.3: Restore and build the scaffold.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 78-84)

### [x] Implementation Phase 2: Define the Upload Contract

<!-- parallelizable: false -->

* [x] Step 2.1: Add options, outcome, and immutable result types.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 89-117)
* [x] Step 2.2: Define the asynchronous `IBlobUploader` seam.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 118-144)
* [x] Step 2.3: Build the contract before implementing consumers.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 145-150)

### [x] Implementation Phase 3: Implement Storage and Policy

<!-- parallelizable: true -->

* [x] Step 3.1: Implement `AzureBlobUploader` over `BlobContainerClient`.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 155-184)
* [x] Step 3.2: Implement `UploadService` validation and result mapping.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 185-218)
* [x] Step 3.3: Add the in-memory fake and policy tests.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 219-250)
* [x] Step 3.4: Build and run non-integration tests with Azurite stopped.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 251-258)

Steps 3.1 and 3.2 can proceed independently after Phase 2. Step 3.3 can begin
with the fake while service tests wait for Step 3.2.

### [x] Implementation Phase 4: Implement the CLI Boundary

<!-- parallelizable: false -->

* [x] Step 4.1: Add local Azurite configuration.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 263-291)
* [x] Step 4.2: Add the internal process runner, parse arguments, compose
  services, and map output and exit codes.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 292-341)
* [x] Step 4.3: Reference the CLI from tests and add focused in-process parser,
  exit-code, and redaction coverage.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 342-374)
* [x] Step 4.4: Build and run all non-integration tests.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 375-382)

Step 4.1 and parser-only work may begin in parallel with Phase 3 after the
contract exists. Composition and phase validation wait for Phase 3.

### [x] Implementation Phase 5: Add Acceptance Assets

<!-- parallelizable: false -->

* [x] Step 5.1: Add a synthetic CSV acceptance fixture.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 387-406)
* [x] Step 5.2: Add `.azurite/` to `.gitignore`.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 407-425)
* [x] Step 5.3: Update only the README acceptance path and obsolete wording.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 426-448)
* [x] Step 5.4: Verify Git state and non-integration tests.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 449-457)

Steps 5.1 and 5.2 can run after Phase 1 in parallel with source implementation.
README finalization waits for the actual CLI contract and output.

### [x] Implementation Phase 6: Add the Azurite Integration Test

<!-- parallelizable: false -->

* [x] Step 6.1: Add one category-gated upload and download round-trip test with
  unique-container cleanup.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 462-485)

### [x] Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

* [x] Step 7.1: Restore, build, and run default tests with Azurite stopped.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 490-499)
* [x] Step 7.2: Start Azurite, run the integration test, and execute the
  documented upload command with executable PowerShell assertions.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 500-526)
* [x] Step 7.3: Verify missing-source, redaction, and non-overwrite behavior.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 527-552)
* [x] Step 7.4: Fix isolated validation issues or report broader blockers.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Lines 553-558)
* [x] Step 7.5: Pin participating Blob clients to the newest REST service
  version shared by the approved SDK and emulator pins, then rerun blocked
  acceptance checks.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Compatibility Correction)
  * Research: `.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md` (Phase 7 Implementation Compatibility Addendum)

### [x] Implementation Phase 8: Address Review Findings

<!-- parallelizable: false -->

* [x] Step 8.1: Correct adapter byte counting for positioned and non-seekable
  readable streams and add focused regression coverage.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Review Remediation)
  * Review: `.copilot-tracking/reviews/2026-10-01/issue-2-walking-skeleton-plan-review.md` (`RV-001`)
* [x] Step 8.2: Remove generated production and test placeholders.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Review Remediation)
  * Review: `.copilot-tracking/reviews/2026-10-01/issue-2-walking-skeleton-plan-review.md` (`RV-003`, `RV-004`)
* [x] Step 8.3: Pin both documented Azurite commands to 3.36.0 and name all
  three deferred product decisions.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Review Remediation)
  * Review: `.copilot-tracking/reviews/2026-10-01/issue-2-walking-skeleton-plan-review.md` (`RV-002`, `RV-005`)
* [x] Step 8.4: Restore, build, run default and integration tests, execute the
  documented CLI path, and reconcile implementation evidence.
  * Details: `.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md` (Review Remediation)

## Planning Log

See `.copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md`
for discrepancy tracking, evaluated implementation paths, and follow-on work.

## Dependencies

* .NET 8 SDK
* Node.js with `npx`, or Docker, for Azurite 3.36.0
* NuGet access for the issue-pinned packages
* Azurite only for integration and manual acceptance validation

## Success Criteria

* A clean clone can start Azurite and upload
  `data/contributions-2026-07.csv` through the documented command. Traces to:
  issue #2 done condition.
* Success output includes blob name, container, and actual byte count and exits
  0. Traces to: issues #1 and #2.
* Missing source and storage failures are actionable, omit stack traces and the
  connection string, and exit 2. Traces to: issue #2 testing criteria.
* `dotnet test --filter "Category!=Integration"` passes with Azurite stopped.
  Traces to: issue #2 testing criteria.
* The opt-in integration test uploads and reads identical bytes through
  Azurite. Traces to: issue #2 testing criteria.
* Existing blobs are not overwritten, while no `--overwrite`, `AlreadyExists`,
  or exit-code-1 contract is introduced. Traces to: DR-01.
* The adapter returns bytes consumed from the stream's current position and
  supports readable non-seekable streams without post-commit length access.
  Traces to: RV-001.
* Clone-to-run commands pin Azurite 3.36.0, and generated placeholders are
  absent from the shipped library and tests. Traces to: RV-002 through RV-004.