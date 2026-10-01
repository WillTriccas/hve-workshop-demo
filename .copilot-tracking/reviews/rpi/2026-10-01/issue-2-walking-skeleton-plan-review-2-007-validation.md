<!-- markdownlint-disable-file -->

# Phase 7 Validation After Phase 8 Remediation

## Validation Status

**Passed**

All five Phase 7 plan items are complete after Phase 8 remediation. The current
workspace preserves the approved SDK and emulator pins, configures every
participating Blob client with REST service version `2025-11-05`, passes the
offline build and default test boundary, and passes the strict Azurite 3.36.0
integration and CLI acceptance paths. No Critical, Major, or Minor finding
remains in the Phase 7 boundary.

## Scope and Evidence Reviewed

Validation covered only Phase 7 at
`.copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md:175-192`
and its detailed requirements at
`.copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md:486-581`.
The evidence set included:

* The complete implementation plan and Planning Log
* The complete changes record
* The complete primary research document, including the Phase 7 compatibility
	addendum
* The complete implementation details and planning log
* The first review and its `RV-001` through `RV-005` resolution conditions
* The opening second-review record and available Phase 8 validation record
* Current source, project configuration, tests, README, Git state, editor
	diagnostics, and independently executed acceptance commands

The plan, changes record, research, details, planning log, reviews, and product
files were read only. This validation record is the only artifact modified by
this validation.

## Plan-to-Change Comparison

| Phase 7 item | Changes-record claim | Verified current evidence | Assessment |
|--------------|----------------------|---------------------------|------------|
| Step 7.1: restore, build, and default tests with Azurite stopped | The changes record reports a warning-free build and 24 default tests after remediation at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:72-75`. | Ports 10000 through 10002 had no listeners and `.azurite` was absent before execution. Restore completed with all projects current, build completed with 0 warnings and 0 errors, and all 24 non-integration tests passed. | Complete |
| Step 7.2: strict emulator round trip and documented CLI upload | The changes record reports 2 Azurite 3.36.0 integration tests and the documented 165-byte upload at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:72-75`. | Exact `azurite@3.36.0` was started without `--skipApiVersionCheck`; ports 10000, 10001, and 10002 bound. Both integration tests passed. The CLI exited 0 and printed `Uploaded contributions-2026-07.csv to contributions (165 bytes)`. | Complete |
| Step 7.3: missing source, redaction, repeat upload, and preserved bytes | The changes record reports redaction and original-byte preservation in the final Phase 7 and Phase 8 checks at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:70-75`. | Missing source exited 2 with actionable text. Repeat upload exited 2 with a collision-specific sanitized message. Combined failure output contained no development connection string, account key, stack trace, or source-line trace. Both integration tests passed again after the collision path; the non-overwrite test requires HTTP 409 and compares downloaded bytes with the original payload at `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:57-108`. | Complete |
| Step 7.4: fix isolated issues or report broader blockers | The changes and planning logs record the compatibility correction and Phase 8 review remediation at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:58-75` and `.copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md:84-115`. | The service-version mismatch was corrected without weakening strict checking. Phase 8 removed the two generated placeholders and corrected the documented emulator pins. Current editor diagnostics report no errors, whitespace checks pass, and no broader product policy was added. | Complete |
| Step 7.5: common service version, fixed dependency pins, strict checking, and acceptance rerun | The changes record states that SDK 12.29.1, Azurite 3.36.0, and Blob API `2025-11-05` remain fixed at `.copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md:68-75`. | The SDK package remains 12.29.1 at `src/SchemeFileUploader/SchemeFileUploader.csproj:4`. The adapter uses one private `V2025_11_05` constant and factory for both adapter methods at `src/SchemeFileUploader/AzureBlobUploader.cs:11-13`, `:27-30`, `:43-58`. Both independent integration clients use the same version at `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:16-22` and `:67-73`. Exact strict Azurite 3.36.0 execution passed. | Complete |

## Service-Version Compatibility

The research identifies `2025-11-05` as the newest REST API version shared by
`Azure.Storage.Blobs` 12.29.1 and Azurite 3.36.0 at
`.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md:69-175`.
Current implementation evidence conforms to that recommendation:

* `Azure.Storage.Blobs` remains pinned to 12.29.1 in
	`src/SchemeFileUploader/SchemeFileUploader.csproj:4`.
* `AzureBlobUploader` keeps the compatible version private rather than adding
	public product configuration at
	`src/SchemeFileUploader/AzureBlobUploader.cs:11-13`.
* `ExistsAsync` and `UploadAsync` both use `CreateContainerClient`, and that
	factory constructs the client with `V2025_11_05` at
	`src/SchemeFileUploader/AzureBlobUploader.cs:27-30`, `:43-58`.
* The two integration tests each construct their independent download and
	cleanup client with `V2025_11_05` at
	`tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs:16-22` and
	`:67-73`.
* Workspace search found no additional directly constructed participating
	`BlobContainerClient` instances outside the adapter factory and those two
	integration clients.
* The README pins npm to `azurite@3.36.0` and Docker to
	`mcr.microsoft.com/azure-storage/azurite:3.36.0` at `README.md:63-69`.
* Independent execution used exact Azurite 3.36.0 without
	`--skipApiVersionCheck`; both integration tests passed twice. Strict runtime
	acceptance therefore confirms that no participating tested path advertises
	the SDK's unsupported default `2026-06-06` version.

## Independent Final Validation Evidence

### Offline boundary

* Precondition: no listener on ports 10000 through 10002 and no `.azurite`
	directory
* `dotnet restore`: passed; all projects were current
* `dotnet build --no-restore`: passed with 0 warnings and 0 errors
* `dotnet test --no-build --filter "Category!=Integration"`: 24 passed, 0
	failed, 0 skipped
* Editor diagnostics: no errors in source or tests

### Strict emulator boundary

* Emulator: exact `azurite@3.36.0`, strict version checking retained
* Endpoint readiness: ports 10000, 10001, and 10002 listening
* First integration run: 2 passed, 0 failed, 0 skipped
* First documented CLI upload: exit 0 with exact blob, container, and 165-byte
	count
* Missing source: exit 2 with actionable sanitized output
* Repeated upload: exit 2 with actionable sanitized collision output
* Failure-output scan: no connection string, account key, stack trace, or
	source-line trace
* Post-collision integration run: 2 passed, 0 failed, 0 skipped

### Cleanup and workspace integrity

* Azurite was stopped after validation
* Ports 10000 through 10002 were released
* Ignored `.azurite` persistence was removed
* `git diff --check` passed
* A trailing-whitespace scan of non-generated source, test, and data files
	passed
* `src/SchemeFileUploader/Class1.cs` and
	`tests/SchemeFileUploader.Tests/UnitTest1.cs` are absent
* The implementation remains untracked in the supplied working tree, so this
	validation assessed current workspace content rather than a committed clean
	clone

## Prior-Review Reconciliation Within Phase 7

| Prior gap | Phase 8 resolution condition | Current Phase 7 result |
|-----------|------------------------------|------------------------|
| `RV-002` / `VAL-007-001`: floating Azurite commands | Pin npm and Docker to 3.36.0 and rerun the acceptance path | Resolved. Both README commands are pinned, and exact strict 3.36.0 execution passed. |
| `RV-003` / `VAL-007-002`: generated library placeholder | Remove `Class1.cs` and rebuild | Resolved. The file is absent and the warning-free build passed. |
| `RV-004` / `VAL-007-002`: no-op generated test inflated totals | Remove `UnitTest1.cs` and reconcile validation totals | Resolved. The file is absent and the intentional default suite now reports 24 passing tests. |
| `RV-001`: adapter byte count could fail after remote commit | Count bytes consumed from positioned and non-seekable streams, with regression tests | Resolved as supporting final-validation evidence. The default tests pass, and the adapter-level Azurite test passes for both stream forms. |
| `RV-005`: deferred decisions were incomplete in README | Name collision, consumer, and destination-ownership decisions | Resolved as supporting acceptance-document accuracy at `README.md:28-37`. |

## Coverage Assessment

* Phase 7 checklist: 5 of 5 complete
* Plan-item coverage: 100%
* Participating Blob clients explicitly configured with `V2025_11_05`: 100%
* Offline suite: 24 passed, 0 failed, 0 skipped
* Strict Azurite integration suite: 2 passed, 0 failed, run twice
* Required CLI paths: success, missing source, and repeated upload all passed
* Required cleanup: complete
* Prior Phase 7 findings: 2 of 2 resolved

## Findings by Severity

### Critical

None.

### Major

None.

### Minor

None.

## Clarifying Questions

None. Available artifacts and current workspace evidence resolve the Phase 7
requirements.

## Recommended Next Validations Not Completed

These checks are outside the Phase 7 acceptance contract and do not qualify the
Passed status:

* [ ] Rehearse the instructions from a committed clean clone after the current
	untracked implementation is committed
* [ ] Exercise the pinned Docker startup alternative in addition to the
	independently validated npm path
* [ ] Capture the wire-level `x-ms-version` header if protocol-level evidence is
	required beyond source coverage and strict-emulator execution

## Conclusion

Phase 7 passes after Phase 8 remediation. The current implementation and
documentation preserve the approved dependency pins, use the researched common
REST service version on every participating client, pass the emulator-free
validation boundary, and pass strict Azurite integration, CLI success,
failure, redaction, collision, byte-preservation, and cleanup checks. No
severity-graded finding remains.
