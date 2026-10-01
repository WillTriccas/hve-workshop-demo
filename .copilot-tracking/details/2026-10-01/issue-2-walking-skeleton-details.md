<!-- markdownlint-disable-file -->
# Implementation Details: Issue 2 Walking Skeleton

## Context Reference

Sources:

* `.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md`
* `.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md`
* `README.md`
* GitHub issue #2: <https://github.com/Azure-Samples/hve-workshop-demo/issues/2>
* GitHub issue #1: <https://github.com/Azure-Samples/hve-workshop-demo/issues/1>

## Implementation Phase 1: Scaffold the Solution

<!-- parallelizable: false -->

### Step 1.1: Create the solution and shared build settings

Create a .NET solution and shared project properties. Target `net8.0` because
the issue explicitly requires it, enable nullable reference types and implicit
usings, and treat compiler warnings as errors. Do not add a Dockerfile or
production deployment assets.

Files:

* `SchemeFileUploader.sln` - Solution containing all three projects
* `Directory.Build.props` - Shared target framework and compiler settings

Discrepancy references:

* `DD-01` - Issue-required .NET 8 overrides the general .NET 10 convention

Success criteria:

* The solution loads with the .NET 8 SDK.
* Shared compiler settings apply to the library, CLI, and test projects.

Dependencies:

* .NET 8 SDK

### Step 1.2: Create projects, references, and pinned dependencies

Create a class library, console application, and xUnit test project. Add the
library reference to both consumers and add the exact package versions inherited
from issue #1. Configure `appsettings.json` to copy to the CLI output directory.
Keep each project flat because each remains below the repository's 16-file
organization threshold.

Files:

* `src/SchemeFileUploader/SchemeFileUploader.csproj` - Library with
  `Azure.Storage.Blobs` 12.29.1
* `src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj` - Executable with
  the library reference, `Microsoft.Extensions.Configuration.Json` 8.0.0, and
  `Microsoft.Extensions.Configuration.Binder` 8.0.0
* `tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj` - Tests with
  the library reference, `xunit` 2.9.3, `Microsoft.NET.Test.Sdk` 18.9.0, and
  `xunit.runner.visualstudio` 2.8.2
* `SchemeFileUploader.sln` - Project registrations

Discrepancy references:

* `DD-02` - Retain the issue-pinned xUnit v2 package despite its deprecation

Success criteria:

* `dotnet restore` resolves all pinned packages.
* The CLI and tests compile against the library.
* No command-line, logging, dependency-injection, mocking, retry, identity, or
  web package is introduced.

Dependencies:

* Step 1.1 completion

### Step 1.3: Validate the scaffold

Validation commands:

* `dotnet restore` - Restore the pinned dependency graph
* `dotnet build --no-restore` - Verify solution wiring and warning policy

## Implementation Phase 2: Define the Upload Contract

<!-- parallelizable: false -->

### Step 2.1: Add options, result, and outcome types

Define `UploaderOptions` with defaults of `UseDevelopmentStorage=true` and
`contributions`. Define an `UploadOutcome` containing only `Uploaded` and
`Failed`, plus an immutable `UploadResult` carrying outcome, blob name,
container, bytes written, and a sanitized message. Document public members with
XML comments and keep namespaces aligned with project names.

Files:

* `src/SchemeFileUploader/UploaderOptions.cs` - Local emulator settings
* `src/SchemeFileUploader/UploadOutcome.cs` - Current public outcomes
* `src/SchemeFileUploader/UploadResult.cs` - Immutable operation result

Discrepancy references:

* `DR-01` - Do not introduce `AlreadyExists` before issue #1 resolves collision
  behavior

Success criteria:

* The model can express successful uploads and sanitized failures.
* No connection string is included in the result model or generated messages.
* Exit-code concerns remain outside the library model.

Dependencies:

* Implementation Phase 1 completion

### Step 2.2: Define the storage seam

Create `IBlobUploader` with asynchronous `ExistsAsync` and `UploadAsync`
methods. Both methods accept container and blob names plus a cancellation token;
`UploadAsync` also accepts a readable stream and explicit `overwrite` value and
returns bytes written. Keep `ExistsAsync` for the next slice but do not invoke
it from issue #2 policy.

Files:

* `src/SchemeFileUploader/IBlobUploader.cs` - Storage abstraction prescribed by
  issue #2

Discrepancy references:

* `DR-01` - Preserve the future collision seam without implementing policy

Success criteria:

* Consumers can test policy without Azure SDK types.
* Every asynchronous operation accepts `CancellationToken`.
* Public interface members have XML documentation.

Dependencies:

* Step 2.1 completion

### Step 2.3: Validate the contract

Validation command:

* `dotnet build --no-restore` - Compile the public contract before consumers

## Implementation Phase 3: Implement Storage and Policy

<!-- parallelizable: true -->

### Step 3.1: Implement the Azure Blob adapter

Implement `AzureBlobUploader` over `BlobContainerClient`. Construct a container
client from the configured connection string and requested container, call
`CreateIfNotExistsAsync` without public access, obtain the blob client, and use
the SDK stream upload overload with the supplied `overwrite` and cancellation
token. Return the uploaded stream length after success. Implement
`ExistsAsync` through the blob client but do not make it part of this slice's
service flow.

Files:

* `src/SchemeFileUploader/AzureBlobUploader.cs` - Azurite-backed storage adapter

Discrepancy references:

* `DR-01` - Pass `overwrite: false` from policy as a transport guard only

Success criteria:

* A clean emulator gains a private container automatically.
* The adapter uploads one blob without buffering it into application memory.
* SDK exceptions and connection details do not become user-facing messages in
  this class.

Dependencies:

* Step 2.2 completion
* `Azure.Storage.Blobs` 12.29.1

### Step 3.2: Implement upload policy and source validation

Implement `UploadService.UploadAsync` against `IBlobUploader`. Reject a blank or
missing source path before storage access, derive the destination with
`Path.GetFileName`, open a readable stream, and upload with `overwrite: false`.
Return the actual byte count and a success message containing the blob and
container names. Convert expected file and Azure request failures into concise,
actionable `Failed` results while allowing cancellation to propagate. Do not
include exception objects, stack traces, request URIs, configuration objects, or
connection values in messages.

Files:

* `src/SchemeFileUploader/UploadService.cs` - File-to-storage orchestration and
  current policy

Discrepancy references:

* `DR-01` - Collision remains a sanitized generic failure with no exit-code-1
  mapping
* `DR-02` - Content, size, symlink, and malware rules remain outside this local
  walking skeleton

Success criteria:

* A readable file reaches the uploader under its base filename.
* Missing or unreadable files return actionable failures with zero bytes.
* Existing blobs are never overwritten.
* `OperationCanceledException` is not converted into a storage failure.

Dependencies:

* Step 2.2 completion

### Step 3.3: Add the in-memory fake and policy tests

Create a dictionary-backed `FakeBlobUploader` that copies stream bytes by
container and blob name and records invocations. Test `UploadService` behavior
with temporary files. Use xUnit behavior names in
`GivenContext_WhenAction_ExpectedResult` form and Arrange, Act, Assert sections.
Cover empty-destination success, exact byte count and destination metadata,
missing source behavior, no fake invocation on validation failure, and
non-overwrite intent.

Files:

* `tests/SchemeFileUploader.Tests/FakeBlobUploader.cs` - Deterministic storage
  fake required by the issue
* `tests/SchemeFileUploader.Tests/UploadServiceTests.cs` - Policy behavior tests

Discrepancy references:

* `DD-03` - Use the issue-required fake instead of the generic preference for
  NSubstitute

Success criteria:

* Policy tests require no emulator or network.
* Temporary files are removed even when assertions fail.
* The fake never receives secret configuration.

Dependencies:

* Step 2.2 completion
* Step 3.2 completion for final test execution

### Step 3.4: Validate storage and policy

Validation commands:

* `dotnet build --no-restore` - Compile adapter and service
* `dotnet test --no-build --filter "Category!=Integration"` - Run fake-backed
  tests with Azurite stopped

## Implementation Phase 4: Implement the CLI Boundary

<!-- parallelizable: false -->

### Step 4.1: Add local configuration

Add an `Uploader` JSON section containing the development storage shorthand and
default `contributions` container. Define the production options-loader delegate
in `Program.cs`: load the required JSON file from the CLI output directory, bind
it to `UploaderOptions`, and reject empty settings by name without printing
their values. Pass this delegate into `UploaderApplication.RunAsync`; do not
invoke it before entering the runner's sanitized exception boundary. The
development shorthand is not a real credential, but it still must not appear in
output.

Files:

* `src/SchemeFileUploader.Cli/appsettings.json` - Local-only Azurite settings
* `src/SchemeFileUploader.Cli/Program.cs` - Production options-loader delegate

Success criteria:

* A normal run requires no Azure subscription, credential, or environment
  variable.
* Configuration reaches the output directory during build and publish.
* Invalid configuration produces a concise exit-2 failure.
* Missing or malformed JSON cannot escape the runner as an unhandled exception.

Dependencies:

* Step 1.2 completion
* Step 2.1 completion

### Step 4.2: Parse arguments and map process behavior

Implement an internal `UploaderApplication.RunAsync` process runner that accepts
the argument array, a `Func<UploaderOptions>` options loader, standard-output
and standard-error `TextWriter` instances, an `IBlobUploader` factory, and a
cancellation token. Invoke the options loader inside the runner's top-level
sanitized exception boundary before constructing storage. Map missing,
malformed, or invalid configuration to an actionable exit-2 error that names
the setting or file but never its value.
Keep explicit parsing for `Usage: uploader <file> [--container <name>]` inside
this boundary. Accept exactly a path and optional container pair. Reject missing
values, unknown switches, duplicate switches, and extra positional arguments
with usage and an actionable message. Construct `UploadService` from the
factory-produced uploader, then print sanitized success or failure output.
Return 0 only for `Uploaded`, return 2 for every current failure, and leave 1
unused. Catch unexpected exceptions at the process boundary without printing
stack traces or exception objects.

Keep top-level `Program.cs` as a thin composition root: define, but do not invoke,
the JSON options loader; construct the real `AzureBlobUploader` factory; pass
both delegates with `Console.Out` and `Console.Error`; and assign the returned
exit code. Add `InternalsVisibleTo` for `SchemeFileUploader.Tests` in the CLI
project rather than making the process runner part of the product's public API.

Files:

* `src/SchemeFileUploader.Cli/Program.cs` - Configuration and production
  composition root
* `src/SchemeFileUploader.Cli/UploaderApplication.cs` - Internal argument
  parser, process runner, output, and exit mapping
* `src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj` - Test-only
  internal visibility declaration

Discrepancy references:

* `DR-01` - Do not add `--overwrite` or use exit code 1
* `DR-03` - Select the in-process CLI test boundary before implementation
* `DR-05` - Keep configuration acquisition inside the sanitized boundary

Success criteria:

* Default and overridden containers reach `UploadService` correctly.
* Usage errors perform no storage operation and return 2.
* Success output includes blob name, container, and actual byte count.
* Standard output and error never contain the connection string or stack trace.

Dependencies:

* Steps 3.1, 3.2, and 4.1 completion

### Step 4.3: Add focused CLI tests

Reference the CLI project from the test project and invoke internal
`UploaderApplication.RunAsync` directly with `StringWriter` output streams,
an options-loader delegate, and a factory returning `FakeBlobUploader`. Test the
valid default path, container override, missing path, missing container value,
unknown argument, output redaction, exit 0 on success, and exit 2 on current
failures. Add focused options-loader cases for a missing JSON file, malformed
JSON, and empty required settings. Use a sentinel connection value or exception
message containing that value and assert it is absent from both captured
streams.

Files:

* `tests/SchemeFileUploader.Tests/ProgramTests.cs` - CLI contract and redaction
  tests
* `tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj` - Add the
  required CLI project reference

Success criteria:

* CLI tests run in process without Azurite.
* The exact configuration value is absent from captured standard output and
  standard error.
* Missing and malformed configuration return 2 with no stack trace or raw
  exception text.
* Exit code 1 remains untested and unused because its policy is deferred.

Dependencies:

* Step 4.2 completion
* Step 3.3 fake available for process wiring

### Step 4.4: Validate the CLI

Validation commands:

* `dotnet build --no-restore` - Compile configuration and process wiring
* `dotnet test --no-build --filter "Category!=Integration"` - Run all local
  policy and CLI tests with Azurite stopped

## Implementation Phase 5: Add Acceptance Assets

<!-- parallelizable: false -->

### Step 5.1: Add synthetic sample data

Create a small CSV with a header and fictional aggregate contribution rows. Do
not include person names, addresses, account identifiers, or plausible personal
data. Tests and output must calculate the real file length rather than assuming
the README's former illustrative value.

Files:

* `data/contributions-2026-07.csv` - Synthetic acceptance fixture

Success criteria:

* The sample is valid text and contains no personal data.
* The documented CLI command can upload it unchanged.

Dependencies:

* None after Implementation Phase 1

### Step 5.2: Ignore emulator persistence

Add only `.azurite/` to the repository ignore rules because the documented
emulator command persists metadata and blob content there. Preserve all existing
Visual Studio ignore entries.

Files:

* `.gitignore` - Repository-local Azurite state exclusion

Success criteria:

* Running the documented Azurite command does not add emulator state to Git
  status.

Dependencies:

* None

### Step 5.3: Update the acceptance-path documentation

Revise `README.md` only where issue #2 makes the greenfield wording obsolete.
Keep the workshop rationale, but replace statements that nothing exists, show
success output with filename, container, and actual byte count, and document
the opt-in integration-test command. Keep the existing Azurite startup and
default non-integration test commands. Do not create the aspirational
`docs/requirements/prd.md` or `docs/lab-brief.md` paths.

Files:

* `README.md` - Truthful clone, test, emulator, and run instructions

Success criteria:

* A new clone can follow the documented commands without undocumented steps.
* Documentation distinguishes the default test run from the Azurite test.
* The workshop's intentionally deferred decisions remain visible.

Dependencies:

* Steps 4.2 and 5.1 completion so commands and output are accurate

### Step 5.4: Validate acceptance assets

Validation commands:

* `git status --short` - Confirm `.azurite/` remains ignored and only intended
  source assets appear
* `dotnet test --no-build --filter "Category!=Integration"` - Confirm sample
  and documentation changes do not disturb local tests

## Implementation Phase 6: Add the Azurite Integration Test

<!-- parallelizable: false -->

### Step 6.1: Implement the opt-in round-trip test

Create one test marked `[Trait("Category", "Integration")]`. Generate a unique
container name, upload a small file through `AzureBlobUploader`, read it back
with `DownloadContentAsync`, and compare bytes. Delete the container in a
`finally` block. Never run this test as part of the default filtered test suite.

Files:

* `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs` - Emulator-backed
  upload and download proof

Success criteria:

* The test creates a missing private container, uploads, reads identical bytes,
  and cleans up.
* The test is discoverable only under the `Integration` category filter.
* A failed assertion still attempts container cleanup.

Dependencies:

* Step 3.1 completion
* Azurite 3.36.0 running on its default Blob endpoint

## Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

### Step 7.1: Validate without Azurite

Stop Azurite and run:

* `dotnet restore`
* `dotnet build --no-restore`
* `dotnet test --no-build --filter "Category!=Integration"`

Success requires no warnings or errors and no attempted emulator connection.

### Step 7.2: Validate the emulator round trip

Start Azurite in a separate process:

* `npx azurite@3.36.0 --silent --location .azurite`

Then run:

```powershell
dotnet test --no-build --filter "Category=Integration"
if ($LASTEXITCODE -ne 0) { throw "Azurite byte round-trip failed." }

$successOutput = & dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv 2>&1 | Out-String
$successExit = $LASTEXITCODE
if ($successExit -ne 0) { throw "Expected upload exit 0, received $successExit.`n$successOutput" }
if ($successOutput -notmatch 'contributions-2026-07\.csv' -or
  $successOutput -notmatch 'contributions' -or
  $successOutput -notmatch '\d[\d,]* bytes') {
  throw "Success output omitted the blob name, container, or byte count.`n$successOutput"
}
```

The integration-test command is the executable blob-byte inspection: it reads
the uploaded blob through the SDK and compares it byte for byte before cleanup.
The CLI assertions verify that the configured `contributions` container receives
the sample and that output includes blob, container, and actual byte count.

### Step 7.3: Validate failure and redaction behavior

Run exact PowerShell assertions for a missing source and a repeated upload:

```powershell
$missingOutput = & dotnet run --project src/SchemeFileUploader.Cli -- data/does-not-exist.csv 2>&1 | Out-String
$missingExit = $LASTEXITCODE
if ($missingExit -ne 2) { throw "Expected missing-file exit 2, received $missingExit." }
if ($missingOutput -match 'UseDevelopmentStorage=true|Stack trace|\sat\s.*\.cs:line') {
  throw "Missing-file output exposed configuration or a stack trace.`n$missingOutput"
}
if ([string]::IsNullOrWhiteSpace($missingOutput)) { throw "Missing-file output was not actionable." }

$repeatOutput = & dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv 2>&1 | Out-String
$repeatExit = $LASTEXITCODE
if ($repeatExit -ne 2) { throw "Expected repeat-upload exit 2, received $repeatExit." }
if ($repeatOutput -match 'UseDevelopmentStorage=true|Stack trace|\sat\s.*\.cs:line') {
  throw "Repeat-upload output exposed configuration or a stack trace.`n$repeatOutput"
}
```

After the repeated upload, rerun the byte round-trip integration test to confirm
storage remains readable. The adapter's `overwrite: false` contract and policy
tests establish that the first blob was not replaced; do not add another command
line dependency solely to inspect private Azurite data.

### Step 7.4: Fix minor issues or report blockers

Apply isolated formatting, compiler, test, and documentation corrections found
during validation. Record any issue that requires broader policy, package, or
architecture changes as follow-on work instead of expanding issue #2.

### Step 7.5: Correct pinned SDK and emulator service-version compatibility

Configure every `BlobContainerClient` created by this slice with
`BlobClientOptions.ServiceVersion.V2025_11_05`. Apply the setting to both
adapter paths and the integration test's independent client. Keep
`Azure.Storage.Blobs` 12.29.1 and Azurite 3.36.0 pinned, retain strict emulator
version checking, and do not expose the service version as product
configuration. Rerun all Phase 7.2 and 7.3 checks from clean emulator state.

Research reference:

* `.copilot-tracking/research/2026-10-01/issue-2-walking-skeleton-research.md`
  - Phase 7 Implementation Compatibility Addendum

Success criteria:

* All participating clients send API version `2025-11-05`.
* The integration round trip passes against Azurite 3.36.0.
* The first CLI upload exits 0 and reports the exact blob, container, and byte
  count.
* Missing-source and repeated-upload checks exit 2 without exposing the
  connection string or a stack trace.
* Both approved dependency pins and all existing contracts remain unchanged.

## Implementation Phase 8: Address Review Findings

### Step 8.1: Correct upload byte counting

Replace post-upload `Stream.Length` access with byte counting that measures the
payload consumed from the stream's current position. For seekable streams,
capture the remaining length before upload. For non-seekable streams, wrap the
source with a read-counting stream that leaves ownership with the caller and
does not access `Length`. Add default, emulator-independent tests for a
positioned seekable stream and a non-seekable readable stream.

Files:

* `src/SchemeFileUploader/AzureBlobUploader.cs` - Correct byte-count mapping
* `src/SchemeFileUploader/CountingReadStream.cs` - Internal non-seekable read
  counter
* `tests/SchemeFileUploader.Tests/CountingReadStreamTests.cs` - Focused stream
  counting coverage
* `tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs` - Adapter-level
  coverage against Azurite
* `src/SchemeFileUploader/SchemeFileUploader.csproj` - Test-only internal
  visibility when required

Success criteria:

* A seekable stream reports `Length - Position` captured before upload.
* A non-seekable stream reports bytes actually read and is not disposed by the
  counting wrapper.
* No `Length` access occurs after a successful remote upload.

### Step 8.2: Remove generated placeholders

Delete `src/SchemeFileUploader/Class1.cs` and
`tests/SchemeFileUploader.Tests/UnitTest1.cs`. The build and discovered test
total must reflect only intentional implementation and behavior tests.

### Step 8.3: Correct acceptance documentation

Pin npm to `azurite@3.36.0` and Docker to
`mcr.microsoft.com/azure-storage/azurite:3.36.0`. Name the unresolved collision
behavior, first consumer, and post-local destination configuration and ownership
decisions without resolving them.

### Step 8.4: Validate and reconcile evidence

Run:

* `dotnet restore`
* `dotnet build --no-restore`
* `dotnet test --no-build --filter "Category!=Integration"`
* `npx azurite@3.36.0 --silent --location .azurite`
* `dotnet test --no-build --filter "Category=Integration"`
* The documented sample upload command and success assertions
* `git diff --check`

Stop Azurite, remove ignored persistence, and update the changes record with
the corrected default test total and review-finding dispositions.

## Dependencies

* .NET 8 SDK
* Node.js with `npx`, or Docker, for Azurite 3.36.0
* Pinned NuGet packages from issues #1 and #2

## Success Criteria

* One documented command uploads the synthetic file to a clean Azurite
  container and reports filename, container, and actual bytes.
* All default tests pass with Azurite stopped.
* The opt-in integration test uploads and reads identical bytes.
* Missing source and storage failures are actionable, sanitized, and return 2.
* No output contains the connection string, SDK exception object, or stack
  trace.
* Existing blobs are not overwritten, while collision product policy remains
  deferred to issue #1.