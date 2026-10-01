<!-- markdownlint-disable-file -->
# Issue 2 Walking Skeleton Phase 4 Validation

## Status

Passed.

Phase 4 implementation coverage is complete. All four Phase 4 checklist steps
are represented in source and project wiring, all 13 focused CLI test cases
passed, the complete 23-test non-integration suite passed, and the actual CLI
returned the required sanitized exit-2 behavior for usage and missing-source
paths. One Minor evidence-record discrepancy does not affect the Phase 4
implementation verdict.

## Scope and Evidence

Validation scope: Issue 2, Implementation Phase 4, "Implement the CLI
Boundary."

Compared artifacts:

* Plan: .copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md:133-150
* Details: .copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md:259-382
* Changes log: .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:31-44 and 55
* Planning log: .copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md:25-47 and 68-97
* Primary research: .copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md:14-50
* Detailed research: .copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md:337-429
* Composition root: src/SchemeFileUploader.Cli/Program.cs:1-69
* Injected runner and parser: src/SchemeFileUploader.Cli/UploaderApplication.cs:1-123
* Configuration: src/SchemeFileUploader.Cli/appsettings.json:1-6
* CLI project wiring: src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj:1-29
* Focused tests: tests/SchemeFileUploader.Tests/ProgramTests.cs:1-289
* Test project wiring: tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj:1-27

Validation performed on 2026-10-01:

* `git diff --check` completed without errors.
* `dotnet build --no-restore` completed with zero warnings and zero errors.
* `dotnet test --no-build --filter "Category!=Integration"` passed all 23 discovered tests.
* The focused `ProgramTests` filter passed all 13 discovered CLI cases.
* The built output contained src/SchemeFileUploader.Cli/bin/Debug/net8.0/appsettings.json with the expected `Uploader` section.
* The actual CLI returned 2 for `--unknown`, printed the exact usage contract, and exposed no connection string or stack trace.
* The actual CLI returned 2 for a missing source, printed an actionable basename-only message, and exposed no connection string or stack trace.

## Phase 4 Requirement Coverage

### Step 4.1: Local configuration

Status: Complete.

* appsettings.json defines `Uploader:ConnectionString` as the development-storage shorthand and `Uploader:DefaultContainer` as `contributions` at src/SchemeFileUploader.Cli/appsettings.json:1-6.
* The CLI project copies the file for build and publish at src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj:22-27. The build-output copy was independently observed.
* `Program.Main` defines an uninvoked `Func<UploaderOptions>` against `AppContext.BaseDirectory` at src/SchemeFileUploader.Cli/Program.cs:7-11, then passes it into the runner at src/SchemeFileUploader.Cli/Program.cs:14-20.
* `LoadOptions` requires the JSON file, converts missing and malformed JSON to safe named errors, validates both required setting names without interpolating their values, and binds the section at src/SchemeFileUploader.Cli/Program.cs:23-68.
* Missing-file, malformed-JSON, and both empty-setting branches are covered at tests/SchemeFileUploader.Tests/ProgramTests.cs:67-143.

### Step 4.2: Parser, injected runner, output, and exits

Status: Complete.

* `UploaderApplication.RunAsync` accepts arguments, the deferred options loader, both writers, an uploader factory, and cancellation at src/SchemeFileUploader.Cli/UploaderApplication.cs:9-18.
* The parser runs first, while options loading occurs inside the same top-level `try` and before factory invocation at src/SchemeFileUploader.Cli/UploaderApplication.cs:20-33. This satisfies planning-log discrepancy DR-05.
* The parser accepts exactly a source path and an optional `--container <name>` pair. It rejects a missing or blank path, a switch in the path position, unknown second arguments, missing or switch-like container values, duplicate container switches, and extra arguments at src/SchemeFileUploader.Cli/UploaderApplication.cs:68-108.
* Invalid syntax writes an actionable error and the exact usage string without loading configuration or constructing storage at src/SchemeFileUploader.Cli/UploaderApplication.cs:22-28.
* Default and overridden containers are passed to `UploadService`; successful results go only to standard output and return 0, while service failures go to standard error and return 2 at src/SchemeFileUploader.Cli/UploaderApplication.cs:30-48.
* Configuration failures, cancellation, and unexpected exceptions all return 2. Unexpected exceptions use fixed text and do not print exception objects or stack traces at src/SchemeFileUploader.Cli/UploaderApplication.cs:50-64. No code path returns 1.
* The production factory constructs `AzureBlobUploader` from loaded options at src/SchemeFileUploader.Cli/Program.cs:11-13.

### Step 4.3: Internal visibility and focused tests

Status: Complete.

* The CLI grants `InternalsVisibleTo` only to `SchemeFileUploader.Tests` at src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj:12-17.
* The test project references both the CLI and library at tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj:21-24.
* The tests invoke the internal runner in process with `StringWriter`, an injected options loader, an injected fake factory, and `CancellationToken.None` at tests/SchemeFileUploader.Tests/ProgramTests.cs:249-276.
* Default-container success verifies exit 0, blob name, container, exact byte count, and empty standard error at tests/SchemeFileUploader.Tests/ProgramTests.cs:35-63.
* Container override verifies the selected destination and exit 0 at tests/SchemeFileUploader.Tests/ProgramTests.cs:9-33.
* The argument theory covers missing path, missing container value, unknown option, duplicate option, and extra positional input, while also proving no configuration load or factory call occurs at tests/SchemeFileUploader.Tests/ProgramTests.cs:144-177 and 227-235.
* Missing-source behavior verifies exit 2, actionable standard error, empty standard output, and no storage invocation at tests/SchemeFileUploader.Tests/ProgramTests.cs:178-197.
* Missing JSON, malformed JSON, and empty required settings verify exit 2 and safe diagnostics at tests/SchemeFileUploader.Tests/ProgramTests.cs:67-143.
* Sentinel-sensitive exceptions and malformed content are absent from both captured streams at tests/SchemeFileUploader.Tests/ProgramTests.cs:98-121 and 198-225. The empty-setting theory also verifies that a present sentinel connection value is absent from both streams at tests/SchemeFileUploader.Tests/ProgramTests.cs:67-94.
* The focused Phase 4 run passed 13 of 13 cases. Every focused scenario listed in Step 4.3 has a matching passing test.

### Step 4.4: Build and non-integration validation

Status: Complete.

* The independent build completed with zero warnings and zero errors.
* The independent non-integration run passed 23 of 23 tests with no Azurite process required.
* These results reproduce the Phase 4 claims in .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:55 and the planning-log validation statement at .copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md:68-97.

## Output and Redaction Assessment

The implementation satisfies the Phase 4 output contract:

* Successful service results are written to standard output and return 0.
* Usage, configuration, source, storage, cancellation, and unexpected failures return 2; exit 1 remains unused.
* Production configuration errors name `appsettings.json` or the required setting, never the setting value.
* Unexpected exceptions are replaced with fixed prose.
* Focused tests inspect both captured streams for the sentinel value.
* Direct CLI checks found no `UseDevelopmentStorage=true`, `AccountKey=`, stack-trace heading, or source-line frame in usage and missing-source output.

## Findings

### Critical

None.

### Major

None.

### Minor

#### MIN-001: Changes log incorrectly records generated placeholders as removed

Expected: The changes log should match the actual workspace when it states that
generated placeholders were removed.

Observed: .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:42-44 records src/SchemeFileUploader/Class1.cs and tests/SchemeFileUploader.Tests/UnitTest1.cs as removed, but both files remain at src/SchemeFileUploader/Class1.cs:1-6 and tests/SchemeFileUploader.Tests/UnitTest1.cs:1-9. The latter contributes one no-op passing test to the reported 23-test total.

Impact: The Phase 4 implementation and its 13 focused tests remain valid, but
the release evidence overstates cleanup and the meaningful test count by one.

Resolution condition: Remove the placeholders in a later implementation stage
or correct the changes log and reported test interpretation. No implementation
artifact was modified during this validation.

## Missing Evidence

No material evidence is missing for a Phase 4 verdict.

The following checks were not repeated in this validation because they are not
required to establish Phase 4 conformance:

* A fresh `dotnet publish` was not executed. Publish-copy behavior is declared at src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj:22-27, while build-copy behavior was observed directly.
* A live successful upload was not repeated because it requires Azurite and belongs to later integration and final-validation phases. The injected success paths were exercised by focused tests, and the planning and changes logs record the later acceptance run.
* Editor diagnostics were not independently queried. The warning-free build provides the relevant compiler evidence.

Clarifying questions: None.

## Conclusion

Phase 4 passes. Deferred configuration loading occurs inside the sanitized
runner boundary; the parser enforces the documented grammar; the injected
runner composes the service without exposing internals publicly; success and
all current failures map to the required streams and 0/2 exits; configured and
exception-carried sensitive values are redacted; test-only internal visibility
and project references are correct; and all focused tests pass.

Coverage assessment: Complete for all four Phase 4 steps and all focused test
scenarios. MIN-001 is a non-blocking evidence-record discrepancy outside the
CLI boundary and does not change the Passed status.
