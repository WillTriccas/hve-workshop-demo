<!-- markdownlint-disable-file -->
# Review: Issue 2 Walking Skeleton

## Executive Summary

* Assessment: The full seven-phase implementation is functionally strong but
  needs rework before acceptance because three Major contract or release-path
  defects and two Minor evidence or documentation defects remain.
* Why this matters: The current CLI path succeeds, but the public upload adapter
  can misreport or fail after a successful upload, the documented emulator path
  is not reproducible, and unintended generated API and test artifacts remain.
* Review execution: Complete
* Assessed outcome: Defects found
* Task Reviewer status: Needs Rework
* Severity counts: 0 Critical, 3 Major, 2 Minor
* Validation coverage: Restore, warning-free build, 23 discovered default tests,
  whitespace checks, and one strict Azurite 3.36.0 integration test were run.
* Confidence and limitations: High confidence for the current workspace. A
  clean-clone rehearsal was not run because the implementation is uncommitted.

## What You May Not Know

* `AzureBlobUploader.UploadAsync` reads `Stream.Length` after the SDK upload.
  That works for the current file stream at position zero, but violates the
  broader readable-stream contract and can throw after a successful remote
  mutation.
* The implementation correctly selects Blob REST API `2025-11-05`, but the
  README uses floating Azurite commands rather than the validated 3.36.0 pin.
* The reported 23 default tests include one empty generated test.

## Findings and Proposed Routes

<!-- rpi:review id=RV-001 -->
### RV-001 Major: Uploaded byte count is not reliable for the public stream contract

`AzureBlobUploader.UploadAsync` uploads from the stream's current position and
then returns `content.Length`.

* Related scope: Phase 3, Steps 3.1 and 3.2; `IBlobUploader.UploadAsync`
* Expected behavior: Return the actual number of bytes written from the supplied
  readable stream.
* Observed behavior and evidence: src/SchemeFileUploader/AzureBlobUploader.cs,
  `UploadAsync`, returns total stream length after upload. For a nonzero stream
  position this overstates uploaded bytes; for a non-seekable stream it can
  throw `NotSupportedException` after the blob has already been committed.
* Impact: Callers can receive incorrect success metadata or retry a remote
  mutation that already succeeded.
* Resolution condition: Correctly count bytes submitted from seekable and
  non-seekable readable streams, with regression tests for a nonzero position
  and a non-seekable stream. Alternatively, explicitly narrow the public
  contract through planning before enforcing a seekable-only precondition.
* Proposed destination: `/task-implement`
* Smallest useful next action: Fix byte counting without changing the accepted
  CLI behavior, then add focused adapter tests.

<!-- rpi:review id=RV-002 -->
### RV-002 Major: README does not pin the accepted Azurite version

* Related scope: Phase 5, Step 5.3; Phase 7 acceptance environment
* Expected behavior: A clean clone uses Azurite 3.36.0, the exact version in the
  plan, compatibility research, and final validation.
* Observed behavior and evidence: README.md uses `npx azurite` and an untagged
  `mcr.microsoft.com/azure-storage/azurite` image.
* Impact: The documented workflow can silently use an emulator version other
  than the tested compatibility target.
* Resolution condition: Pin npm and Docker commands to 3.36.0 and rerun the
  documented acceptance path.
* Proposed destination: `/task-implement`
* Smallest useful next action: Update both README commands and validate them.

<!-- rpi:review id=RV-003 -->
### RV-003 Major: Generated public placeholder remains in the library

* Related scope: Phases 1 and 2; public API documentation requirement
* Expected behavior: Remove generated placeholders and document every
  intentional public member.
* Observed behavior and evidence: src/SchemeFileUploader/Class1.cs still exposes
  undocumented public `Class1`; the changes log records that file as removed.
* Impact: The shipped assembly has an unintended undocumented public API and
  the implementation evidence is inaccurate.
* Resolution condition: Remove `Class1`, or replace it with an intentional,
  meaningful, documented type, then rebuild.
* Proposed destination: `/task-implement`
* Smallest useful next action: Remove the generated placeholder.

<!-- rpi:review id=RV-004 -->
### RV-004 Minor: Empty generated test inflates validation totals

* Related scope: Phases 1, 3, 4, and 7 validation evidence
* Expected behavior: Generated placeholders are removed and reported test totals
  represent behavior-validating tests.
* Observed behavior and evidence: tests/SchemeFileUploader.Tests/UnitTest1.cs
  contains an empty passing test and remains despite the changes log recording
  its removal.
* Impact: The 23-test claim overstates meaningful default coverage by one and
  conflicts with the changes record.
* Resolution condition: Remove the generated test and reconcile the changes log
  and validation count.
* Proposed destination: `/task-implement`
* Smallest useful next action: Delete the placeholder and rerun default tests.

<!-- rpi:review id=RV-005 -->
### RV-005 Minor: README does not identify all deferred decisions

* Related scope: Phase 5, Step 5.3
* Expected behavior: Keep the workshop's intentionally deferred decisions
  visible without resolving them.
* Observed behavior and evidence: README.md states that three design decisions
  remain but explains only collision behavior. The other deferred consumer and
  post-local destination decisions are not identified.
* Impact: Workshop readers cannot discover the complete refinement boundary
  from the repository documentation alone.
* Resolution condition: Name all three deferred decisions concisely while
  preserving their unresolved status.
* Proposed destination: `/task-implement`
* Smallest useful next action: Add the two missing decision topics to the
  existing backlog explanation.

## Parent Decision Record

### Current Disposition

* Based on events: RD-001 through RD-008
* Review execution: Complete
* Final outcome: Defects found; Task Reviewer status is Needs Rework
* Finding decisions and next actions: RV-001 through RV-005 are accepted as
  review findings and routed together to a bounded `/task-implement` pass.
* Decisions still needed: None for routing; implementation judgment remains for
  the stream-counting mechanism.

### Decision History

| Event | Subject | Decision source | Status or value | Proposed destination | Final destination | Owner | More information needed | Smallest next action | Rationale |
|-------|---------|-----------------|-----------------|----------------------|-------------------|-------|-------------------------|----------------------|-----------|
| RD-001 | participation | system | reviewer-owned under Task Reviewer workflow | none | none | reviewer | none | Complete comparison | The Task Reviewer workflow requires a completed status and handoff. |
| RD-002 | walkthrough | system | not required by Task Reviewer workflow | none | none | reviewer | none | Record routes | Findings are delivered as a review handoff. |
| RD-003 | RV-001 | reviewer | accepted | `/task-implement` | `/task-implement` | implementor | none | Correct byte counting and test stream variants | Public contract correctness defect. |
| RD-004 | RV-002 | reviewer | accepted | `/task-implement` | `/task-implement` | implementor | none | Pin README commands | Reproducible acceptance-path defect. |
| RD-005 | RV-003 | reviewer | accepted | `/task-implement` | `/task-implement` | implementor | none | Remove public placeholder | Public API and evidence defect. |
| RD-006 | RV-004 | reviewer | accepted | `/task-implement` | `/task-implement` | implementor | none | Remove no-op test and update totals | Test-evidence defect. |
| RD-007 | RV-005 | reviewer | accepted | `/task-implement` | `/task-implement` | implementor | none | Name deferred decisions | Documentation completeness defect. |
| RD-008 | outcome | reviewer | Needs Rework / Defects found | none | `/task-implement` | implementor | none | Address RV-001 through RV-005 | Three Major findings prevent acceptance. |

## Validation Evidence

| Command | Scope | Status | Summary |
|---------|-------|--------|---------|
| `dotnet restore` | Full solution | Passed | All three projects were current. |
| `dotnet build --no-restore` | Full solution | Passed | Zero warnings and zero errors. |
| `dotnet test --no-build --filter "Category!=Integration"` | Default suite without Azurite | Passed with qualification | 23 discovered tests passed; one is the no-op test in RV-004. |
| `git diff --check` | Tracked working-tree changes | Passed | No whitespace errors reported. |
| First integration attempt | Azurite integration | Failed, superseded | Test ran before the emulator completed endpoint binding and received connection refusal. |
| `dotnet test --no-build --filter "Category=Integration"` | Ready Azurite 3.36.0 | Passed | One integration test passed after all endpoints reported ready. |
| Emulator cleanup | Local process and persistence | Passed | Azurite stopped, port 10000 released, and `.azurite` removed. |
| Editor diagnostics | Workspace | Passed | No compiler or editor errors reported. |

## Phase Validation Synthesis

| Phase | Status | Evidence summary | Findings |
|-------|--------|------------------|----------|
| Phase 1: Scaffold | Partial | Solution and project wiring build successfully with accepted pins and warning policy. | RV-003 and RV-004: generated placeholders remain despite removal claims. |
| Phase 2: Contract | Partial | Options, outcomes, result, and seam match the planned shape with XML docs and cancellation. | RV-003: unintended undocumented public `Class1`. |
| Phase 3: Storage and policy | Partial | Private creation intent, non-overwrite policy, sanitization, cancellation, fake, and policy tests are present. | RV-001: adapter byte-count contract is incorrect for general readable streams. |
| Phase 4: CLI | Passed | Deferred configuration loading, exact parsing, redaction, injected runner, and 0/2 exits are covered. | RV-004 qualifies the aggregate test count but does not invalidate CLI behavior. |
| Phase 5: Acceptance assets | Partial | CSV, ignore rule, exact output, and test split are correct. | RV-002 and RV-005 affect reproducibility and deferred-decision visibility. |
| Phase 6: Integration test | Passed | Unique container, adapter upload, 409 non-overwrite, byte comparison, and cleanup are implemented. | No phase-specific defect beyond RV-001's adapter contract. |
| Phase 7: Final validation | Partial | Offline and ready-emulator checks pass; compatibility correction is justified. | RV-002 leaves the documented version inconsistent; RV-003 and RV-004 make the changes evidence inaccurate. |

Detailed phase evidence is stored under
.copilot-tracking/reviews/rpi/2026-10-01/issue-2-walking-skeleton-plan-001-validation.md
through
.copilot-tracking/reviews/rpi/2026-10-01/issue-2-walking-skeleton-plan-007-validation.md.

## Implementation Quality Findings by Category

* Correctness and API design: RV-001 is Major. Current CLI uploads remain
  correct because they use a position-zero `FileStream`.
* Architecture: The planned library, CLI, seam, fake, and integration boundaries
  are preserved.
* Error handling and security: User-facing failures are sanitized, cancellation
  propagates through the library, and private-container creation intent is
  explicit.
* Documentation and operations: RV-002 and RV-005 require correction.
* Public API quality: RV-003 requires correction. Enabling `CS1591` is optional
  preventive hardening, not an additional finding.
* Tests: Substantive policy, CLI, and integration coverage is strong; RV-004
  corrects the generated no-op. An explicit anonymous-access denial assertion is
  optional strengthening, not an acceptance defect.

The full quality report is stored at
.copilot-tracking/reviews/quality/2026-10-01/issue-2-walking-skeleton-plan-quality.md.

## Risks, Blockers, and Residual Work

* Blockers: None blocked review execution. RV-001 through RV-003 block task
  acceptance.
* Missing work and deviations: Correct the five findings and reconcile the
  changes log after implementation. DD-01 through DD-04 remain justified.
* Deferred from original scope:
  * WI-01: Decide refuse, replace, or version collision behavior.
  * WI-02: Define source-file safety policy.
  * WI-03: Design production Azure identity and ingestion controls.
  * WI-04: Evaluate xUnit v3 migration.
* Discovered during review:
  * Optional: Assert effective anonymous-access denial in integration coverage.
  * Optional: Enable XML documentation generation and `CS1591` enforcement.

## Review Record

### Scope and Evidence

* Task ID: issue-2-walking-skeleton
* Review date: 2026-10-01
* Review scope: Full task, Implementation Phases 1 through 7
* Assessed boundary: .NET 8 scaffold, public upload contract, Azure adapter and
  policy, CLI boundary, acceptance assets, Azurite integration, final validation,
  and the compatibility correction
* Review depth and provenance: Standard; default from the task-review request
* Candidate identity: Current uncommitted workspace and dated artifact set
* Review execution: Complete
* Helper use: Seven RPI Validator runs supplied phase candidates. An initial
  Implementation Validator run was blocked by helper file access; a second run
  completed from exact source evidence supplied by the review parent. The review
  parent independently verified every recorded finding in workspace files.
* Plan: .copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md
* Plan critique: Skipped; no critique artifact was supplied or discovered
* Changes: .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md
* Research: .copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md
* Planning log: .copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md
* Details: .copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md
* Other evidence considered: Workspace source, tests, README, configuration,
  conversation validation history, and independently rerun commands

### Opening Review State

* Interpreted review goal: Determine whether the completed Issue 2 implementation
  conforms to accepted plans and evidence, and route substantive gaps.
* Review scope: Full task
* Evidence readiness: Complete enough for a credible verdict
* Acceptance basis: Plan success criteria, phase details, research decisions,
  documented deviations, C# standards, and Markdown standards
* First comparison boundary: Seven phase comparisons followed by full-quality
  assessment and independent validation commands
* Active read-only boundaries: Only review artifacts were created or updated;
  product source, plan, research, changes, and details were not modified
* Authority: The review parent compared evidence and recorded the outcome and
  routes under the Task Reviewer workflow.
* Initial blockers: None

### Acceptance and Change Coverage

| Requirement or scope | Implementation and validation evidence | Assessment | Finding or rationale |
|----------------------|----------------------------------------|------------|----------------------|
| Phase 1 scaffold and pins | Solution, project files, build | Gap | RV-003 and RV-004 |
| Phase 2 public contract | Options, result, outcome, interface, build | Gap | Contract types conform; unintended `Class1` violates API requirement |
| Phase 3 storage and policy | Adapter, service, fake, policy tests | Gap | RV-001 |
| Phase 4 CLI boundary | Program, runner, config, CLI tests | Met | Parser, configuration boundary, redaction, and exits conform |
| Phase 5 acceptance assets | CSV, README, `.gitignore`, default tests | Gap | RV-002 and RV-005 |
| Phase 6 integration proof | Integration test and independent rerun | Met | Unique cleanup and original-byte preservation pass |
| Phase 7 validation and correction | Research addendum, API pin, offline and emulator checks | Gap | Runtime evidence passes; documentation and evidence drift remain |
| Material update DD-04 | Adapter and test use `V2025_11_05`; strict Azurite 3.36.0 test passes | Met | Evidence-backed justified divergence |

### Critique and Follow-Up Assessment

* Latest critique dispositions: Critique skipped; absence was not a blocker.
* Material revisions: DD-04 and Step 7.5 are supported by compatibility research
  and independent integration validation.
* Dependent-work pause assessment: Validation resumed only after research and
  the plan, details, planning log, and changes evidence recorded the correction.
* Justification assessment: DD-01 through DD-04 are supported; RV-001 through
  RV-005 are implementation or evidence defects rather than accepted deviations.

| Follow-up item | Why outside immediate scope | Owner or next action | Assessment and route |
|----------------|-----------------------------|----------------------|----------------------|
| WI-01 collision product policy | Parent issue deliberately defers behavior | Product planning | Open, distinct follow-up |
| WI-02 source-file safety | Requires consumer and environment decisions | Product and security planning | Open, distinct follow-up |
| WI-03 production Azure design | Local workshop excludes production hosting | Architecture and security planning | Open, distinct follow-up |
| WI-04 xUnit v3 | Modernization is unrelated to walking-skeleton acceptance | Engineering maintenance | Open, distinct follow-up |

### Reviewer Self-Check

* [x] Every supplied requirement, acceptance criterion, in-scope marker,
  material update, validation result, blocker, remaining item, and plan follow-up
  has an assessment or explicit gap.
* [x] Findings are substantive, evidence-grounded, severity-graded, and use
  stable `RV-xxx` IDs with expected and observed behavior, a resolution
  condition, and one proposed route each.
* [x] Execution status, assessed outcome, validation coverage, limitations, and
  proposed routes are complete and internally consistent.
* [x] The summary is scoped and advisory, findings keep supporting context
  together, and acceptance coverage distinguishes defects from optional work.
* [x] Standard review assessed the material boundary while omitting cosmetic or
  low-value feedback.
* [x] The review did not mutate product source or implementation-owned tracking
  artifacts and verified helper candidates against workspace evidence.
* Checked boundary: All seven phases, DD-01 through DD-04, WI-01 through WI-04,
  source, tests, configuration, documentation, and validation claims
* Missing or limited evidence: No clean-clone rehearsal; this does not prevent
  the Needs Rework verdict because source-level defects are directly observed.
