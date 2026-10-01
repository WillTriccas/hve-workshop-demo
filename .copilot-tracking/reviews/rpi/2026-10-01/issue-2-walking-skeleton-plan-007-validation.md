<!-- markdownlint-disable-file -->
# Phase 7 Validation: Issue 2 Walking Skeleton

## Status

**Partial**

Phase 7's implementation and executable acceptance behavior passed independent
validation. One major documentation and reproducibility gap prevents a Passed
status: the clone-to-run README does not preserve the approved Azurite 3.36.0
pin even though the compatibility correction and recorded release evidence say
that both pins remain fixed. One minor cleanup and evidence-accuracy gap also
remains because two generated placeholder files recorded as removed still
exist.

## Scope and evidence

Validated the Phase 7 through-line defined at
`.copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md:175-193`
and detailed at
`.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md:486-585`.
The comparison included:

* Plan checklist and success criteria:
	`.copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md:175-222`
* Phase details and executable PowerShell assertions:
	`.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md:486-585`
* Compatibility correction research:
	`.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md:69-232`
* Original issue, package-pin, test, and validation research:
	`.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md:70-134`,
	`:263-279`, and `:531-558`
* Planning-log discrepancy and recorded validation evidence:
	`.copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md:61-109`
* Changes-log claims:
	`.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:20-45`
	and `:50-76`
* Current source, configuration, tests, README, Git state, and runtime behavior

No implementation artifact, plan, research document, details document, changes
log, planning log, or parent review was modified. This validation record is the
only durable file created or updated by the validation.

## Plan-to-change comparison

| Plan item | Changes-log match | Verified implementation and execution | Assessment |
|-----------|-------------------|---------------------------------------|------------|
| Step 7.1: restore, warning-free build, and default tests with Azurite stopped | The changes log records restore, warning-free build, and 23 default tests at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:60` | Before execution, port 10000 was not listening and `.azurite` was absent. `dotnet restore` reported all projects current, `dotnet build --no-restore` completed with 0 warnings and 0 errors, and the filtered suite passed 23 of 23 tests. No Azurite listener was present. | Complete |
| Step 7.2: strict Azurite round trip and CLI success | The changes log records the integration test and first upload at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:60` | Started exact `azurite@3.36.0` without `--skipApiVersionCheck`. The integration test passed 1 of 1. The CLI exited 0 and emitted `Uploaded contributions-2026-07.csv to contributions (165 bytes)`. The fixture is 165 bytes and the output contained the blob, container, and exact byte count. | Complete |
| Step 7.3: missing source, redaction, repeated upload, and non-overwrite bytes | The changes log records all behaviors at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:60` | Missing source exited 2 with actionable text. Repeated upload exited 2 with a sanitized collision message. Neither stream contained the development connection string, an account key, a stack trace, or a source-line trace. The integration test then passed again. Its replacement attempt uses different bytes, requires HTTP 409, downloads the blob, and compares it with the original bytes at `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:11-56`. | Complete |
| Step 7.4: isolated fixes or broader blockers | The changes and planning logs record the service-version mismatch and correction at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:58-60` and `.copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md:61-84` | The mismatch was researched rather than hidden by weakening emulator checks. No broader architecture or collision policy was added. The current source compiles and has no editor diagnostics. | Complete |
| Step 7.5: common service version, exact pins, strict checking, and rerun | The changes log claims both approved pins remain at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:67-75` | The SDK remains 12.29.1 at `src/SchemeFileUploader/SchemeFileUploader.csproj:4`. The adapter centralizes `V2025_11_05` and applies it to both adapter paths at `src/SchemeFileUploader/AzureBlobUploader.cs:10-12`, `:27-31`, `:41-53`, and `:58-63`. The independent integration client uses the same version at `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:17-24`. Strict Azurite 3.36.0 execution passed. The README command is not pinned, so documented pin preservation is incomplete. | Partial |

## Verified behavior

### Offline and emulator independence

* Port 10000 was released and `.azurite` was absent before the offline run.
* Restore completed successfully with the approved package graph.
* Build completed with 0 warnings and 0 errors.
* `Category!=Integration` passed 23 of 23 tests without an emulator.
* The integration test remains explicitly gated by
	`[Trait("Category", "Integration")]` at
	`tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:8-10`.

### Service-version correction and strict checking

* `Azure.Storage.Blobs` remains exactly 12.29.1 at
	`src/SchemeFileUploader/SchemeFileUploader.csproj:4`.
* The production adapter uses one private compatibility constant set to
	`BlobClientOptions.ServiceVersion.V2025_11_05` at
	`src/SchemeFileUploader/AzureBlobUploader.cs:10-12`.
* Both `ExistsAsync` and `UploadAsync` flow through the same factory at
	`src/SchemeFileUploader/AzureBlobUploader.cs:27-31`, `:41-53`, and `:58-63`.
* The integration test's separately constructed client uses the same explicit
	service version at
	`tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:17-24`.
* Validation started exact Azurite 3.36.0 without
	`--skipApiVersionCheck`; startup confirmed listeners on ports 10000, 10001,
	and 10002. Both integration executions passed. This reproduces the research
	recommendation at
	`.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md:155-175`.

### CLI success, failure, and redaction

* Success exited 0 and reported the exact blob name, container, and 165-byte
	count. The success mapping is implemented at
	`src/SchemeFileUploader.Cli/UploaderApplication.cs:31-41`.
* Missing source exited 2 with `Source file 'does-not-exist.csv' was not found.`
	and no storage call. Source validation occurs before opening storage at
	`src/SchemeFileUploader/UploadService.cs:26-42`.
* Repeated upload exited 2 with a collision-specific but contract-compatible
	sanitized message. HTTP 409 mapping is at
	`src/SchemeFileUploader/UploadService.cs:70-77`.
* The runner prints neither unexpected exception objects nor stack traces at
	`src/SchemeFileUploader.Cli/UploaderApplication.cs:44-59`.
* In-process tests also inject a sentinel connection value and a sensitive
	unexpected exception, then assert redaction at
	`tests/SchemeFileUploader.Tests/ProgramTests.cs:52-77`, `:80-132`, and
	`:198-225`.

### Non-overwrite bytes

* Policy always requests `overwrite: false` at
	`src/SchemeFileUploader/UploadService.cs:47-53`.
* The adapter forwards the value to the SDK at
	`src/SchemeFileUploader/AzureBlobUploader.cs:41-53`.
* The unit test verifies the non-overwrite argument at
	`tests/SchemeFileUploader.Tests/UploadServiceTests.cs:130-151`.
* The emulator test uploads `[0, 1, 2, 3, 255]`, attempts replacement with
	`[5, 6, 7]`, requires HTTP 409, downloads the stored blob, and asserts the
	original bytes at
	`tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:11-56`.
* The independently rerun integration test passed both before and after the
	manual repeated-upload check.

### Cleanup

* Every unique integration container is deleted in a `finally` block at
	`tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:57-60`.
* After validation, Azurite was stopped, ports 10000 through 10002 had zero
	listeners, and `.azurite` was removed.
* `.azurite/` is ignored at `.gitignore:425`.
* `git diff --check` completed without whitespace errors.

## Coverage

* Phase 7 checklist coverage: 4 complete, 1 partial
* Executable acceptance checks: 100% passed
* Participating Blob clients with explicit `V2025_11_05`: 100%
* Required offline test result: 23 passed, 0 failed, 0 skipped
* Required integration result: 1 passed, 0 failed, run twice
* CLI paths exercised: success, missing source, repeated upload
* Runtime cleanup: complete
* Documentation and recorded-change accuracy: partial

## Findings by severity

### Critical

None.

### Major

#### VAL-007-001: Documented Azurite command does not preserve the approved pin

Expected:

The compatibility correction requires preserving Azurite 3.36.0, retaining
strict API-version checking, and rerunning against the exact pin. The detailed
command is `npx azurite@3.36.0 --silent --location .azurite` at
`.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md:500-505`.
The research likewise identifies Azurite 3.36.0 as the local-tool prerequisite
and exact validation command at
`.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md:263-279`
and `:531-546`.

Observed:

The clone-to-run README uses `npx azurite --silent --location .azurite` at
`README.md:58-61` and an untagged Docker image at `README.md:64-66`. Both resolve
a moving emulator version rather than the approved 3.36.0 pin. This conflicts
with the changes-log statement that the solution retains Azurite 3.36.0 at
`.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:67-75`.
The implementation was independently validated against exact 3.36.0, so this is
not a demonstrated runtime defect in the current code. It is a reproducibility
and compatibility-maintenance deviation in the documented acceptance path.

Resolution condition:

Pin both documented emulator startup alternatives to Azurite 3.36.0, or revise
the accepted plan and changes evidence to state and validate a deliberate
floating-version policy. Do not add `--skipApiVersionCheck`.

### Minor

#### VAL-007-002: Generated placeholders remain despite recorded removal

Expected:

The changes log records removal of `src/SchemeFileUploader/Class1.cs` and
`tests/SchemeFileUploader.Tests/UnitTest1.cs` at
`.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:47-50`.
The research's expected file inventory omits both at
`.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md:472-509`.

Observed:

`src/SchemeFileUploader/Class1.cs:1-6` still defines an empty public `Class1`,
and `tests/SchemeFileUploader.Tests/UnitTest1.cs:1-10` still defines a vacuous
passing `Test1`. The latter is included in the reported 23-test total, so the
recorded count includes one generated test that proves no behavior.

Resolution condition:

Remove both generated placeholders and rerun the warning-free build and default
test suite, or correct the changes log and justify retaining them.

## Gaps and clarifying questions

* No current evidence captures the actual `x-ms-version` request header on the
	wire. Structural coverage of every directly constructed client plus a passing
	strict Azurite 3.36.0 run provides strong behavioral evidence for
	`2025-11-05`, but packet- or server-log-level confirmation was not required by
	the plan and was not added.
* The source and test directories are currently untracked in Git. Validation
	therefore assessed the working tree supplied by the user, not committed
	history or a clean-clone checkout.
* No clarification is required to assess Phase 7. A maintainer decision is
	needed only if the unpinned README command is intentional despite the exact
	compatibility contract.

## Conclusion

The Phase 7 implementation is functionally sound under the validated boundary.
Offline restore, warning-free build, emulator-independent tests, exact pinned
Azurite execution, strict service-version compatibility, integration byte
round-trip, CLI success, missing-source failure, repeated-upload failure,
redaction, non-overwrite preservation, and runtime cleanup all passed.

The phase remains **Partial** because the public acceptance instructions do not
preserve the Azurite 3.36.0 pin and therefore do not fully match the researched
compatibility correction or changes-log claim. The remaining placeholder files
also make the release record and test count inaccurate. No implementation
artifact was changed during this validation.
