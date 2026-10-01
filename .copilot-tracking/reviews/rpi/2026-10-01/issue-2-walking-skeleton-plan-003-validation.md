<!-- markdownlint-disable-file -->
# Phase 3 Validation: Issue 2 Walking Skeleton

## Validation State

* Phase: Implementation Phase 3, Implement Storage and Policy
* Status: Partial
* Scope: Azure Blob adapter, upload policy, in-memory fake, policy tests, and Phase 3 validation claims
* Validation date: 2026-10-01
* Implementation mutation: None

## Evidence Reviewed

Planning and implementation evidence was read in full for the Phase 3 through-line:

* .copilot-tracking/plans/2026-10-01/issue-2-walking-skeleton-plan.instructions.md
* .copilot-tracking/details/2026-10-01/issue-2-walking-skeleton-details.md
* .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md
* .copilot-tracking/plans/logs/2026-10-01/issue-2-walking-skeleton-log.md
* .copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md
* .copilot-tracking/reviews/2026-10-01/issue-2-walking-skeleton-plan-review.md
* src/SchemeFileUploader/AzureBlobUploader.cs
* src/SchemeFileUploader/UploadService.cs
* src/SchemeFileUploader/IBlobUploader.cs
* src/SchemeFileUploader/UploaderOptions.cs
* src/SchemeFileUploader/UploadOutcome.cs
* src/SchemeFileUploader/UploadResult.cs
* src/SchemeFileUploader/SchemeFileUploader.csproj
* tests/SchemeFileUploader.Tests/FakeBlobUploader.cs
* tests/SchemeFileUploader.Tests/UploadServiceTests.cs
* tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs
* tests/SchemeFileUploader.Tests/ProgramTests.cs
* tests/SchemeFileUploader.Tests/UnitTest1.cs
* src/SchemeFileUploader/Class1.cs

Independent commands completed during validation:

* `dotnet build --no-restore`: passed with zero warnings and zero errors
* `dotnet test --no-build --filter "Category!=Integration"`: passed 23 of 23 discovered cases
* `dotnet test --no-build --filter "FullyQualifiedName~SchemeFileUploader.Tests.UploadServiceTests"`: passed 9 of 9 discovered policy cases
* Test discovery confirmed that the 23-case total includes the no-op `UnitTest1.Test1` placeholder
* Source searches confirmed that production calls to `ExistsAsync` occur only inside `AzureBlobUploader.ExistsAsync`, not in `UploadService`
* Source searches found no application-side buffering operation in src/SchemeFileUploader

## Requirement Evidence

| Phase 3 requirement | Status | Verified evidence |
|---------------------|--------|-------------------|
| Construct `BlobContainerClient` from the configured connection string and requested container | Met | `AzureBlobUploader.CreateContainerClient` uses `_connectionString`, the method's `container`, and the researched compatible REST service version in src/SchemeFileUploader/AzureBlobUploader.cs. |
| Create a missing private container | Met in source; behavioral ACL assertion absent | `AzureBlobUploader.UploadAsync` calls `CreateIfNotExistsAsync` with `PublicAccessType.None` in src/SchemeFileUploader/AzureBlobUploader.cs. The integration test creates a unique missing container, but it does not retrieve container properties and assert anonymous access is disabled. |
| Stream directly to the SDK without application buffering | Met | `AzureBlobUploader.UploadAsync` passes the supplied stream directly to `BlobClient.UploadAsync`. No `MemoryStream`, `ReadAllBytes`, `ToArray`, or application-side copy exists in src/SchemeFileUploader. The fake buffers intentionally for deterministic test inspection. |
| Forward overwrite and cancellation arguments to Azure Storage | Met | `AzureBlobUploader.UploadAsync` forwards `overwrite` and `cancellationToken`; container creation and `ExistsAsync` also receive the cancellation token. tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs proves `overwrite: false` rejects replacement and preserves original bytes. |
| Return the number of bytes actually uploaded | Partial | The local-file path reports the expected value because `UploadService` opens a new position-zero `FileStream`. The adapter returns `content.Length`, which is total stream length rather than bytes consumed from the initial position and can throw after a successful upload for a readable non-seekable stream. See Major finding MAJ-001. |
| Keep SDK exceptions and connection details out of adapter-generated user messages | Met | `AzureBlobUploader` does not create user-facing messages or catch and interpolate SDK exceptions. `UploadService` maps Azure failures to fixed text. |
| Reject blank or missing source paths before storage access | Met | `UploadService.UploadAsync` returns a failed zero-byte result before calling the uploader. Blank and missing-source tests assert no upload invocation. |
| Derive the destination from `Path.GetFileName` | Met | `UploadService.UploadAsync` derives `blobName` with `Path.GetFileName`; the success policy test verifies destination metadata and exact bytes. |
| Open a readable source stream and request `overwrite: false` | Met | `UploadService.UploadAsync` uses `File.OpenRead` and calls the seam with `overwrite: false`. `GivenReadableSource_WhenUploading_RequestsNonOverwritingUpload` verifies the policy argument. |
| Preserve cancellation | Met | `UploadService` explicitly rethrows `OperationCanceledException`; `GivenCanceledUpload_WhenUploading_PropagatesCancellation` passes a canceled token through the fake and observes cancellation. |
| Sanitize file, collision, and Azure request failures | Met for specified cases | The service catches unreadable-file exceptions and `RequestFailedException`, returns zero bytes, and never includes exception objects, request URIs, connection strings, or stack traces. The sensitive-request-failure test and collision test pass. |
| Do not call `ExistsAsync` from issue 2 policy | Met in current source; regression guard absent | `UploadService` has no `ExistsAsync` call. The only production use is the implementation inside `AzureBlobUploader.ExistsAsync`. The fake does not record existence checks, so policy tests would not detect a future preflight call. See Minor finding MIN-001. |
| Provide a dictionary-backed fake with invocation tracking and exact-byte storage | Met | tests/SchemeFileUploader.Tests/FakeBlobUploader.cs stores copied bytes by container and blob, rejects replacement when overwrite is false, propagates cancellation during copy, and records upload invocations. |
| Run Phase 3 without Azurite | Met | The build and filtered non-integration suite passed in this session without starting Azurite. Nine meaningful `UploadServiceTests` cases passed. |

## Findings

### Critical

None.

### Major

#### MAJ-001: Adapter byte count does not satisfy the stream contract for all readable streams

Expected:

* `IBlobUploader.UploadAsync` returns the number of bytes written.
* Phase 3 accepts a readable stream, uploads it through the SDK stream overload, and reports actual bytes.

Observed:

* `AzureBlobUploader.UploadAsync` returns `content.Length` after the SDK call.
* For a seekable stream whose initial `Position` is nonzero, the SDK uploads the remaining content while the adapter reports the full stream length.
* For a readable non-seekable stream, accessing `Length` can throw `NotSupportedException` after the blob upload has already succeeded. That creates a reported failure after a committed storage mutation.
* The current `UploadService` local-file path masks the issue by always opening a position-zero `FileStream`, and current tests only use position-zero seekable streams.

Evidence:

* src/SchemeFileUploader/IBlobUploader.cs
* src/SchemeFileUploader/AzureBlobUploader.cs
* src/SchemeFileUploader/UploadService.cs
* tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs

Impact:

* The walking-skeleton CLI reports the correct byte count for its current local-file path, but the public seam and adapter do not reliably return bytes written as specified.
* A future seam consumer can receive incorrect metadata or retry after an upload that actually succeeded.

Resolution condition:

* Calculate uploaded bytes from the initial and final position when seeking is supported, or use another count that represents bytes consumed, and define or support non-seekable behavior without throwing after a successful upload.
* Add focused coverage for a nonzero initial stream position and the chosen non-seekable-stream contract.

### Minor

#### MIN-001: No policy test prevents an `ExistsAsync` preflight regression

Current source conforms to the research decision: `UploadService` does not call `ExistsAsync`. The fake records only uploads, however, and the existing-destination test would still pass if policy later added an existence preflight and returned the same sanitized failure. The requirement is verified statically but is not protected behaviorally.

Evidence:

* .copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md
* src/SchemeFileUploader/UploadService.cs
* tests/SchemeFileUploader.Tests/FakeBlobUploader.cs
* tests/SchemeFileUploader.Tests/UploadServiceTests.cs

Resolution condition:

* Record existence invocations in the fake and assert zero calls for upload policy paths.

#### MIN-002: Private-container behavior is asserted by implementation shape, not by a test

The adapter explicitly supplies `PublicAccessType.None`, so current source meets the clean-container requirement. The Azurite integration test verifies upload, collision rejection, and byte preservation but does not query container properties or otherwise assert that anonymous access remains disabled.

Evidence:

* src/SchemeFileUploader/AzureBlobUploader.cs
* tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs

Resolution condition:

* Extend integration coverage to assert that the created container has no public access level.

#### MIN-003: The changes log incorrectly claims generated placeholders were removed

The changes log lists src/SchemeFileUploader/Class1.cs and tests/SchemeFileUploader.Tests/UnitTest1.cs as removed. Both files are present. The no-op `UnitTest1.Test1` is also included in the claimed passing totals: the current 23-test suite contains 22 substantive cases plus that placeholder, and the Phase 3-era total of 10 corresponds to 9 policy cases plus the placeholder.

Evidence:

* .copilot-tracking/changes/2026-10-01/issue-2-walking-skeleton-changes.md
* src/SchemeFileUploader/Class1.cs
* tests/SchemeFileUploader.Tests/UnitTest1.cs
* Current non-integration test discovery and execution

Resolution condition:

* Remove the generated placeholders as claimed, then rebuild and restate validation totals using substantive discovered cases.

## Gaps and Clarifying Questions

### Validation gaps

* Azurite was not started during this Phase 3 validation. The existing integration source was inspected, but the changes log's emulator execution claim was not independently rerun in this session.
* Streaming and private-container behavior were verified primarily by source inspection. Existing tests do not use a non-seekable or offset stream and do not inspect the container access level.
* No test directly exercises `AzureBlobUploader.ExistsAsync`; this method is deliberately outside the Issue 2 service flow.

### Clarifying questions

None required to assess the accepted Phase 3 scope. The intended stream contract should be made explicit when resolving MAJ-001.

## Coverage Assessment

Phase 3 coverage is high but incomplete.

* Step 3.1, Azure adapter: Mostly covered. Client construction, private creation intent, direct stream forwarding, overwrite behavior, cancellation forwarding, REST-version compatibility, and exception ownership are visible in source. Actual-byte reporting is only correct for the current position-zero seekable file path.
* Step 3.2, upload policy: Covered. Validation, base-filename derivation, zero-byte failures, non-overwrite intent, cancellation propagation, collision behavior, and fixed-message sanitization are implemented and exercised.
* Step 3.3, fake and policy tests: Mostly covered. Nine meaningful cases pass and cover the named policy behaviors. No test detects `ExistsAsync` preflight use, private ACL state, offset/non-seekable byte counting, or direct adapter cancellation.
* Step 3.4, emulator-free validation: Covered for the current tree. The solution builds with zero warnings and errors, and all 23 discovered non-integration cases pass with Azurite stopped. One passing case is a no-op generated placeholder.

Requirement disposition:

* Met: 11
* Partially met: 1
* Verified in source but missing a regression assertion: 2
* Not met: 0

## Conclusion

Phase 3 is Partial. The implemented local-file walking skeleton currently performs sanitized, cancellation-aware, non-overwriting, direct-stream uploads into a privately requested container without using `ExistsAsync` as policy. The source and nine meaningful policy-test cases substantiate those behaviors, and the current emulator-free build and test suite pass.

Acceptance is not a full pass because `AzureBlobUploader.UploadAsync` does not reliably return bytes actually written for the readable-stream contract, and several important claims are protected only by source shape rather than regression tests. The changes record also contains a demonstrably incorrect placeholder-removal claim and counts a no-op test among its passing totals. No Critical finding blocks the current position-zero local-file CLI path, but MAJ-001 should be corrected before treating the storage seam as conformant.
