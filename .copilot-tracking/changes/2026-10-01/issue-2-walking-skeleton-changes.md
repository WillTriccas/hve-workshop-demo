<!-- markdownlint-disable-file -->
# Release Changes: Issue 2 Walking Skeleton

**Related Plan**: issue-2-walking-skeleton-plan.instructions.md
**Implementation Date**: 2026-10-01

## Summary

Implemented the first end-to-end .NET 8 file upload slice with a private
Azurite container, sanitized CLI behavior, emulator-independent policy tests,
and opt-in byte round-trip integration tests. Review findings `RV-001` through
`RV-005` are remediated and validated.

## Changes

### Added

* [SchemeFileUploader.sln](../../../SchemeFileUploader.sln) - Registers the library, CLI, and test projects.
* [Directory.Build.props](../../../Directory.Build.props) - Applies shared .NET 8, nullable, implicit-using, and warning policy settings.
* [src/SchemeFileUploader/SchemeFileUploader.csproj](../../../src/SchemeFileUploader/SchemeFileUploader.csproj) - Defines the Azure Blob-backed library and pinned SDK dependency.
* [src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj](../../../src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj) - Defines the executable, configuration packages, library reference, and configuration copy behavior.
* [src/SchemeFileUploader.Cli/Program.cs](../../../src/SchemeFileUploader.Cli/Program.cs) - Provides the initial CLI entry point for later composition.
* [tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj](../../../tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj) - Defines the xUnit test project and library reference.
* [src/SchemeFileUploader/UploaderOptions.cs](../../../src/SchemeFileUploader/UploaderOptions.cs) - Defines local Azurite defaults without exposing configuration in results.
* [src/SchemeFileUploader/UploadOutcome.cs](../../../src/SchemeFileUploader/UploadOutcome.cs) - Defines the current uploaded and failed outcomes.
* [src/SchemeFileUploader/UploadResult.cs](../../../src/SchemeFileUploader/UploadResult.cs) - Carries immutable sanitized upload metadata.
* [src/SchemeFileUploader/IBlobUploader.cs](../../../src/SchemeFileUploader/IBlobUploader.cs) - Defines the asynchronous, cancellation-aware storage seam.
* [src/SchemeFileUploader/AzureBlobUploader.cs](../../../src/SchemeFileUploader/AzureBlobUploader.cs) - Streams non-overwriting uploads through private Azure Blob containers using the newest REST version shared by the pinned SDK and emulator.
* [src/SchemeFileUploader/CountingReadStream.cs](../../../src/SchemeFileUploader/CountingReadStream.cs) - Counts bytes consumed from non-seekable readable streams without taking ownership.
* [src/SchemeFileUploader/UploadService.cs](../../../src/SchemeFileUploader/UploadService.cs) - Validates sources and maps upload results to sanitized outcomes while preserving cancellation.
* [tests/SchemeFileUploader.Tests/FakeBlobUploader.cs](../../../tests/SchemeFileUploader.Tests/FakeBlobUploader.cs) - Provides deterministic in-memory blob storage and invocation tracking.
* [tests/SchemeFileUploader.Tests/UploadServiceTests.cs](../../../tests/SchemeFileUploader.Tests/UploadServiceTests.cs) - Covers validation, metadata, byte preservation, non-overwrite intent, sanitization, and cancellation.
* [src/SchemeFileUploader.Cli/appsettings.json](../../../src/SchemeFileUploader.Cli/appsettings.json) - Supplies local Azurite defaults for clone-to-run behavior.
* [src/SchemeFileUploader.Cli/UploaderApplication.cs](../../../src/SchemeFileUploader.Cli/UploaderApplication.cs) - Implements argument parsing, sanitized process behavior, and exit-code mapping.
* [tests/SchemeFileUploader.Tests/ProgramTests.cs](../../../tests/SchemeFileUploader.Tests/ProgramTests.cs) - Covers CLI parsing, configuration failures, output redaction, container selection, and exit codes in process.
* [data/contributions-2026-07.csv](../../../data/contributions-2026-07.csv) - Provides a 165-byte synthetic aggregate fixture with no personal data.
* [tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs](../../../tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs) - Proves upload, rejected replacement, original-byte preservation, download, and cleanup against Azurite.
* [tests/SchemeFileUploader.Tests/CountingReadStreamTests.cs](../../../tests/SchemeFileUploader.Tests/CountingReadStreamTests.cs) - Covers positioned and non-seekable stream byte accounting and ownership.

### Modified

* [src/SchemeFileUploader.Cli/Program.cs](../../../src/SchemeFileUploader.Cli/Program.cs) - Composes deferred configuration loading and the Azure Blob adapter inside the process runner.
* [src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj](../../../src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj) - Grants test-only internal visibility and carries configuration to output.
* [tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj](../../../tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj) - References the CLI for in-process boundary tests.
* [.gitignore](../../../.gitignore) - Excludes repository-local Azurite persistence.
* [README.md](../../../README.md) - Documents the implemented clone-to-upload path, exact output, default tests, and opt-in integration command.
* [src/SchemeFileUploader/SchemeFileUploader.csproj](../../../src/SchemeFileUploader/SchemeFileUploader.csproj) - Grants test-only access to internal stream accounting behavior.
* [src/SchemeFileUploader/AzureBlobUploader.cs](../../../src/SchemeFileUploader/AzureBlobUploader.cs) - Returns consumed bytes from the initial stream position and supports non-seekable input without post-commit length access.
* [tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs](../../../tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs) - Validates positioned and non-seekable uploads through the real adapter.

### Removed

* `src/SchemeFileUploader/Class1.cs` - Removed the generated library placeholder.
* `tests/SchemeFileUploader.Tests/UnitTest1.cs` - Removed the generated test placeholder.

## Additional or Deviating Changes

* Phase 8 was added from the completed review. It corrects byte-count semantics,
  removes generated placeholders, pins documented emulator commands, and makes
  all deferred workshop decisions visible.

* Phase 1 validation passed: `dotnet restore` and `dotnet build --no-restore` completed with zero warnings and zero errors.
* Phase 2 validation passed: `dotnet build --no-restore` completed with zero warnings and zero errors.
* Phase 3 validation passed: `dotnet build --no-restore` completed with zero warnings and zero errors; 10 non-integration tests passed.
* Phase 4 validation passed: warning-free build, 23 non-integration tests, `git diff --check`, and editor diagnostics all passed.
* Phase 5 validation passed: the fixture is 165 bytes, `.azurite/` ignore behavior was verified, and 23 non-integration tests passed.
* Phase 6 validation passed: warning-free build, 23 non-integration tests, and exact discovery of one integration test.
* Initial Phase 7 emulator validation exposed an API-version mismatch: SDK 12.29.1 defaults to `2026-06-06`, while Azurite 3.36.0 supports through `2025-11-05`.
  * Research confirmed an explicit `BlobClientOptions.ServiceVersion.V2025_11_05` is the smallest correction that preserves both approved pins and strict emulator checks.
* Phase 7 final validation passed after the compatibility correction: restore, warning-free build, 23 default tests, the integration test, first upload, missing-source behavior, repeated-upload redaction, and original-byte preservation all passed.
* Final emulator cleanup passed: Azurite stopped, port 10000 was released, and ignored persistence was removed.
* Phase 8 validation passed: restore; warning-free build; 24 default tests; 2
  Azurite 3.36.0 integration tests; documented 165-byte CLI upload; whitespace
  checks; editor diagnostics; emulator shutdown; persistence removal; and port
  release all passed.

## Release Summary

All eight phases and 28 steps are complete. The implementation has 23 active
repository files: 21 added implementation, test, configuration, build, and
sample-data files plus 2 modified repository files. Two generated placeholder
files were removed before the scaffold became the final implementation.

The solution retains .NET 8, `Azure.Storage.Blobs` 12.29.1, and Azurite 3.36.0.
Blob clients explicitly use REST API version `2025-11-05`, the newest version
supported by both pinned components. No production infrastructure or deployment
changes are included. Upload byte counts now reflect bytes consumed from the
initial stream position, including non-seekable streams. Start Azurite with the
documented pinned command before running the opt-in integration tests or CLI
acceptance path; the default filtered test suite requires no emulator.
