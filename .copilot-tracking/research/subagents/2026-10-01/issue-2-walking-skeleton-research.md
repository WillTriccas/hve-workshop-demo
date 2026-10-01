<!-- markdownlint-disable-file -->
# Issue 2 Walking Skeleton Implementation Research

## Executive Summary

* Research status: Complete
* Retrieval date: 2026-10-01
* Repository: `Azure-Samples/hve-workshop-demo`
* Issue: [#2, Build the walking skeleton: upload a file to a container](https://github.com/Azure-Samples/hve-workshop-demo/issues/2)
* Parent: [#1, Provide a command line tool for uploading files to Azure Storage](https://github.com/Azure-Samples/hve-workshop-demo/issues/1)
* Research posture: Focused, because the caller supplied a bounded issue, named evidence sources, explicit exclusions, and a single permitted write path
* Confidence: High for issue, repository, SDK, package, configuration, and Azurite facts

Issue #2 is an intentionally narrow, three-hour walking skeleton. It creates a
`.NET 8` solution with a library, console application, and xUnit test project;
accepts one local file path and an optional container override; binds the Azurite
connection and default container from configuration; creates the container on a
clean emulator; uploads the file; and reports a sanitized result with exit code
0 or 2. It is not the place to decide overwrite behavior or to add a browser,
cloud identity, production Azure, scanning, infrastructure, or general upload
hardening.

The safest resolution of the collision ambiguity is to preserve the issue's
empty-container precondition, include the parent-prescribed `ExistsAsync` and
`overwrite` seam, and call the SDK with `overwrite: false`. That is a
non-destructive transport guard, not a completed collision policy: the service
does not preflight existence, does not return `AlreadyExists`, and does not use
reserved exit code 1. A collision remains an actionable generic failure until
the parent decision is made in the next slice.

## What You May Not Know

* Issue #2 was renamed from "Add --overwrite flag and AlreadyExists outcome to
  the uploader." Its current body explicitly removes that policy from this
  slice. The rename is strong evidence against reintroducing `--overwrite`.
* Issue #2 is the sole child of #1, but GitHub reports no formal blocked-by or
  blocking relationship. Its only logical dependency is the inherited target
  shape and constraints; the open overwrite decision is intentionally deferred.
* Both issues have zero comments, no assignees, no milestone, no reactions, and
  no linked pull request. There is no hidden comment-level clarification.
* Upstream `main` contains only `.gitignore` and `README.md`. The local checkout
  adds ignored research evidence only. No solution, project, application code,
  tests, sample data, or requirement document exists yet.
* The parent's reference to `docs/requirements/prd.md` is aspirational. That
  exact file is absent, so implementation must not invent its content.
* The issue-pinned `xunit` 2.9.3 package now carries a NuGet deprecation notice
  because xUnit feature development moved to v3. Honor the explicit v2 pin for
  this bounded slice; treat migration as a separate maintenance decision.
* Azurite persists data under the path supplied to `--location`. The README uses
  `.azurite`, but the current `.gitignore` does not exclude it, so the acceptance
  run would otherwise leave emulator files visible as untracked content.

## Findings

### Issue 2 Record

The issue record retrieved on 2026-10-01 is:

| Field | Exact value |
|-------|-------------|
| Title | `Build the walking skeleton: upload a file to a container` |
| URL | <https://github.com/Azure-Samples/hve-workshop-demo/issues/2> |
| State | Open, with no state reason |
| Type | Task |
| Label | `enhancement` |
| Author | `bindsi`, repository member |
| Created | `2026-08-16T17:23:17Z` |
| Updated | `2026-08-16T17:31:57Z` |
| Assignees | None |
| Milestone | None |
| Locked | No |
| Comments | 0 |
| Parent | Issue #1 |
| Sub-issues | 0 |
| Dependencies | 0 blocked-by, 0 blocking |
| Estimate | S, 3 hours, set at 09:50 |

#### Exact Current Body

```markdown
<!-- markdownlint-disable-file -->
## What

Build the walking skeleton of the uploader: one command that takes a file path,
uploads it to a container, and reports what happened.

This is the first slice of #1. It deliberately does not answer what happens
when the blob already exists, because that decision is still open on the
parent.

## Why this slice first

It proves the whole path end to end, from argument parsing through configuration
binding to a real write against the emulator. Every later slice is a policy
change on top of a path that already works.

## How

- Create the solution, the library project, the CLI project, and the test
  project against `net8.0`
- Define `IBlobUploader` with `ExistsAsync` and `UploadAsync`, and implement it
  over `BlobContainerClient`
- Add `UploadService` with an `UploadAsync` that opens the file, writes it, and
  returns an `UploadResult`
- Bind `UploaderOptions` from `appsettings.json` with
  `UseDevelopmentStorage=true`
- Parse `<file>` and optional `--container` in `Program.cs`
- Return exit code 0 on success and 2 on failure, leaving 1 reserved
- Create the container if it does not exist, so a first run on a clean emulator
  works
- Keep the connection string out of every message and log line

Assume the container is empty. Existing blob handling is out of scope for this
slice.

## Out of scope

Existing blob handling, retry and backoff, chunked upload, SAS generation,
server side encryption, container lifecycle policies, real Azure
authentication.

## Testing

- [ ] Policy test against the `IBlobUploader` fake: an upload into an empty
  container succeeds and reports the byte count
- [ ] Policy test: a missing source file fails with an actionable message, not
  a stack trace
- [ ] Policy test: no captured output contains the connection string
- [ ] The opt-in Azurite integration test uploads and then reads the blob back
- [ ] `dotnet test --filter "Category!=Integration"` passes with no emulator
  running

## Done when

Someone who has just cloned the repository can start Azurite, run one command,
and see the file land in the container.

## Estimate

S, 3 hours (set at 09:50).
```

The body defines the work as one command that takes a file path, uploads it to a
container, and reports the result. Its reason for going first is to prove the
complete path from argument parsing, through configuration binding, to a real
write against Azurite.

The exact implementation constraints in its `How` section are:

* Create solution, library, CLI, and test projects targeting `net8.0`
* Define `IBlobUploader.ExistsAsync` and `IBlobUploader.UploadAsync`
* Implement the storage seam over `BlobContainerClient`
* Add `UploadService.UploadAsync` to open the file, write it, and return an
  `UploadResult`
* Bind `UploaderOptions` from `appsettings.json`, using
  `UseDevelopmentStorage=true`
* Parse `<file>` and optional `--container` in `Program.cs`
* Return 0 for success and 2 for failure; reserve 1
* Create the container when absent so a clean Azurite instance works
* Keep the connection string out of all messages and logs
* Assume an empty container and leave existing-blob handling out of scope

Issue #2 has no separately named `Acceptance criteria` heading. Its normative
checks are under `Testing` and `Done when`:

* A fake-backed policy test proves an empty-container upload succeeds and
  reports byte count.
* A missing source file produces an actionable message rather than a stack
  trace.
* Captured output contains no connection string.
* An opt-in Azurite integration test uploads and reads the blob back.
* `dotnet test --filter "Category!=Integration"` passes without Azurite.
* A new clone can start Azurite, run one command, and observe the blob in the
  container.

The timeline records Task assignment, the `enhancement` label, parent #1, a
cross-reference from #1, and the rename away from an overwrite-focused task.

### Parent Issue 1 Context

Issue #1 asks for a repeatable, scriptable command that uploads a contributions
file and leaves an unambiguous outcome. Its first-release acceptance criteria
require a named local file, configured destination, blob/container/byte success
output, actionable failure without a stack trace, distinct success/failure exit
codes, no credential material in output, and emulator-only end-to-end operation.

Three parent decisions remain explicitly open:

1. Collision behavior: refuse, replace, or version the blob name.
2. First consumer: interactive person or unattended scheduler.
3. Post-local destination configuration and ownership.

Issue #2 inherits only the settled portion. It reserves exit code 1 for a future
refusal outcome, uses prose and exit codes 0/2, and configures only local
Azurite. It must not settle any of the three decisions by adding prompts,
machine-readable output, environment matrices, cloud credentials, or a user
visible overwrite mode.

The parent's security lenses are questions, not acceptance criteria for #2:
credential handling, HTTP emulator versus real transport, public container
access, user-supplied paths and symlinks, unbounded file size, and least
privilege. Issue #2 directly resolves only one: credential material must never
appear in output. The other lenses should remain recorded follow-ons unless a
minimal local guard is needed to avoid accidental harm.

### Current Repository and README Target

Upstream commit `f39214dfe05704445a85e43a44cf12208ccced89` and the local checkout
contain these tracked files only:

```text
.gitignore
README.md
```

The README headings and implementation-relevant references are:

* `# Scheme File Uploader`
* `## Why this repository exists`
* `## The backlog`
* `## Getting started`
* `## Target shape`
* `## Planned repository layout`
* `## A note on the connection string`
* `## Contributing`
* `## Trademarks`

`Getting started` requires the .NET 8 SDK plus Node.js or Docker and gives the
two Azurite startup commands. `Target shape` gives `dotnet build`, the
non-integration test filter, the exact `dotnet run` command, and illustrative
success output. `Planned repository layout` names the library, CLI, test, data,
and documentation areas. The test prose requires policy tests behind
`IBlobUploader` plus one opt-in Azurite test. The connection-string note states
that the development shorthand is not a real credential.

No implementation exists. The README also contains future-tense statements
that become inaccurate after #2, including "The tool does not exist yet,"
"There is nothing to run yet," and "Once the first slice lands." A minimal
README update is therefore part of making the clone-and-run acceptance path
truthful. No new documentation tree is required.

## Recommended Minimal Design

### Solution and Projects

Use the parent-prescribed three-project shape:

```text
SchemeFileUploader.sln
Directory.Build.props
src/
  SchemeFileUploader/
    SchemeFileUploader.csproj
  SchemeFileUploader.Cli/
    SchemeFileUploader.Cli.csproj
tests/
  SchemeFileUploader.Tests/
    SchemeFileUploader.Tests.csproj
```

`Directory.Build.props` sets `net8.0`, implicit usings, nullable reference types,
and warnings as errors. The CLI references the library. The test project
references the library; it may also reference the CLI only if output-to-exit-code
wiring is extracted into a testable public type. Keep the sub-16-file projects
flat.

### Packages

Honor the versions pinned by #1:

* Library: `Azure.Storage.Blobs` 12.29.1
* CLI: `Microsoft.Extensions.Configuration.Json` 8.0.0 and
  `Microsoft.Extensions.Configuration.Binder` 8.0.0
* Tests: `xunit` 2.9.3, `Microsoft.NET.Test.Sdk` 18.9.0, and
  `xunit.runner.visualstudio` 2.8.2
* Local tool prerequisite: Azurite 3.36.0 through `npx` or the published Docker
  image; do not add an npm project only to pin it unless reproducible local tool
  acquisition becomes an explicit requirement

Do not add command-line, logging, mocking, DI-container, retry, identity, web,
or scanning packages. The CLI grammar is small enough to parse explicitly, and
the issue requires an in-memory fake rather than an SDK mock.

### Interfaces and Types

Create the parent-prescribed public surface:

```csharp
public interface IBlobUploader
{
    Task<bool> ExistsAsync(
        string container,
        string blobName,
        CancellationToken cancellationToken);

    Task<long> UploadAsync(
        string container,
        string blobName,
        Stream content,
        bool overwrite,
        CancellationToken cancellationToken);
}

public enum UploadOutcome
{
    Uploaded,
    Failed
}

public sealed record UploadResult(
    UploadOutcome Outcome,
    string BlobName,
    string Container,
    long BytesWritten,
    string Message);

public sealed class UploaderOptions
{
    public string ConnectionString { get; init; } = "UseDevelopmentStorage=true";
    public string DefaultContainer { get; init; } = "contributions";
}
```

`AzureBlobUploader` constructs `BlobContainerClient` from the connection string
and container, calls `CreateIfNotExistsAsync` with its default
`PublicAccessType.None`, obtains the `BlobClient`, calls
`UploadAsync(stream, overwrite, cancellationToken)`, and returns the input
stream length after successful completion. In the exact 12.29.1 source,
`overwrite: false` applies `If-None-Match: *` and an existing blob produces a
`RequestFailedException`; `overwrite: true` removes that condition. The same
version's container method leaves an existing container unchanged and returns
`null` for that case. `UploadService` owns local-file validation, derives the
blob name with `Path.GetFileName`, opens the stream only after validation, calls
the uploader, and maps expected failures to `UploadResult`. `ExistsAsync` is
present for the next slice but is not called in #2.

### CLI Contract and Configuration

Keep the inherited command contract exactly:

```text
Usage: uploader <file> [--container <name>]
```

Accepted forms are the path alone or the path followed by one
`--container <name>` pair. Missing values, unknown switches, duplicate
container switches, or extra positional arguments print usage plus a concise
action and exit 2. There are no prompts and no `--overwrite` option.

`src/SchemeFileUploader.Cli/appsettings.json` contains:

```json
{
  "Uploader": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "DefaultContainer": "contributions"
  }
}
```

Copy it to the output directory. Bind the `Uploader` section and reject an
empty connection string or container with a configuration error that names the
setting, not its value. The command-line container overrides only
`DefaultContainer`; it does not mutate configuration.

### File and Blob Semantics

The issue allows the following minimal validation:

* Reject a missing or blank path before storage access.
* Reject a path for which `File.Exists` is false with an actionable message.
* Derive the blob name from `Path.GetFileName(path)` and reject an empty result.
* Let `File.OpenRead` enforce read permission and sharing behavior, then map its
  expected exception to a sanitized failure.
* Do not impose an extension allowlist, size ceiling, directory sandbox, symlink
  policy, CSV parser, hash, archive policy, or malware check in #2.

Create the configured container if absent and upload one block blob named after
the source filename. Pass `overwrite: false` to preserve the empty-container
assumption without destroying data. Do not call `ExistsAsync`, introduce an
`AlreadyExists` result, or assign exit code 1. A collision is reported as a
sanitized failure with exit 2 and text explaining that the destination already
contains the name and that collision policy is not yet supported.

Success uses the result data rather than SDK response text. The parent requires
blob name, container, and byte count, so prefer:

```text
Uploaded contributions-2026-07.csv to contributions (2,048 bytes)
```

The README's shorter illustrative line omits the container; implementation
should satisfy the stronger parent criterion and update that example.

### Result and Error Model

Keep `UploadOutcome` to `Uploaded` and `Failed`. Use one internal stable reason
classification only if it makes mapping clearer, such as `Usage`, `Source`,
`Configuration`, or `Storage`; do not expose a larger public outcome model.

The CLI maps `Uploaded` to exit 0 and every current failure to 2. Exit 1 remains
unused. Expected source, configuration, and Azure request failures become one
line of actionable text. Unexpected exceptions are caught at the process
boundary, produce a generic failure without stack trace or exception object,
and return 2. Cancellation should propagate rather than be misreported as a
storage error.

Never interpolate the connection string, configuration object, request URI,
SDK exception object, or stack trace into console output. Tests should capture
both standard output and standard error and assert absence of the exact configured
connection string.

### Test Split and Sample Data

Use a hand-written dictionary-backed `FakeBlobUploader`, not a mocking package.
Its upload method copies the stream, stores bytes by `(container, blobName)`, and
returns the copied length.

Required non-integration tests:

* Empty fake destination returns `Uploaded`, correct blob/container/byte data,
  and success message.
* Missing source returns `Failed`, an actionable message, zero bytes, and never
  invokes the fake uploader.
* Captured CLI output and error do not contain the configured connection string.
* Focused parser tests cover the valid default-container path, optional
  override, missing path, missing container value, and unknown argument.

The single `[Trait("Category", "Integration")]` test uses a unique container,
uploads a small temporary or sample file through `AzureBlobUploader`, calls
`DownloadContentAsync` on the resulting blob, compares its in-memory
`BinaryData` bytes, and deletes the unique container in a `finally` block. The
official API recommends this method for small blobs that fit in memory, which
matches the tiny test fixture. The test is opt-in and must not affect the
default filtered run.

Create `data/contributions-2026-07.csv` with a tiny synthetic header and a few
fictional aggregate rows. Keep it free of names, identifiers, addresses, account
details, or other plausible personal data. Its exact byte count need not be the
README's illustrative 2,048 bytes; output and tests use the actual stream length.

### Documentation Limited to Acceptance

Modify only `README.md`. Preserve its workshop rationale and existing headings,
but remove obsolete statements that no tool exists, change `Target shape` to an
actual run path, show success output including the container, and state the
opt-in integration-test command and Azurite prerequisite. Do not create
`docs/requirements/prd.md`, `docs/lab-brief.md`, architecture documents,
runbooks, cloud setup, or security guidance under #2.

## Ambiguity Resolution and Scope Guard

| Ambiguity | Resolution for issue #2 | Deferred production or follow-on work |
|-----------|-------------------------|---------------------------------------|
| Existing blob | Assume empty; send `overwrite: false`; collision is generic exit-2 failure | Choose refuse, replace, or version; define exit 1 and command surface |
| Human versus scheduler | Non-interactive prose plus stable 0/2 codes already specified | Structured output, prompts, and richer error taxonomy after consumer decision |
| Destination environments | JSON configuration for Azurite only | Environment ownership, precedence, secrets, managed identity, and cloud accounts |
| Container naming | Default `contributions`; optional CLI override | Naming governance and per-environment provisioning |
| Source path | Accept any readable local file path and use basename as blob name | Root restrictions, symlink policy, canonicalization policy, and watch folders |
| File limits and content | No new policy beyond readable regular file behavior | Size limits, type validation, CSV schema, malware scanning, quarantine, retention |
| Azure security | Do not leak the development connection string | Entra ID, RBAC, HTTPS, network isolation, private containers, Defender, SAS policy |
| Upload mechanics | One SDK stream upload suitable for the small sample | Explicit transfer tuning, large-file block strategy, retries, resume, checksums |

Browser upload, an HTTP API, Entra authentication, Defender scanning,
production Azure, infrastructure-as-code, lifecycle policy, and broader security
work are excluded because both issues place them outside the first slice. No
evidence supports expanding #2 into those areas.

## Expected File Changes

Create these files in dependency order:

```text
SchemeFileUploader.sln
Directory.Build.props
src/SchemeFileUploader/SchemeFileUploader.csproj
src/SchemeFileUploader/IBlobUploader.cs
src/SchemeFileUploader/AzureBlobUploader.cs
src/SchemeFileUploader/UploadService.cs
src/SchemeFileUploader/UploadOutcome.cs
src/SchemeFileUploader/UploadResult.cs
src/SchemeFileUploader/UploaderOptions.cs
src/SchemeFileUploader.Cli/SchemeFileUploader.Cli.csproj
src/SchemeFileUploader.Cli/Program.cs
src/SchemeFileUploader.Cli/appsettings.json
tests/SchemeFileUploader.Tests/SchemeFileUploader.Tests.csproj
tests/SchemeFileUploader.Tests/FakeBlobUploader.cs
tests/SchemeFileUploader.Tests/UploadServiceTests.cs
tests/SchemeFileUploader.Tests/ProgramTests.cs
tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs
data/contributions-2026-07.csv
```

Modify only:

```text
README.md
.gitignore
```

The `.gitignore` change is exactly one entry, `.azurite/`, because Microsoft
documents that Azurite persists metadata and content beneath `--location`, and
the existing acceptance command chooses that repository-local path. Do not
create the README's planned `docs/` files under this issue.

## Dependency Order and Parallelization

1. Scaffold solution, shared build properties, and three projects; add project
   references and pinned packages.
2. Implement result/options/interface types before their consumers.
3. Implement `AzureBlobUploader` and the fake independently once the interface
   compiles.
4. Implement `UploadService` against the interface, then its policy tests.
5. Implement CLI parsing, configuration, output, and exit mapping, then focused
   CLI tests.
6. Add sample data and update only the acceptance-path README text.
7. Run unit/build validation without Azurite.
8. Start Azurite, run the integration test, then run the documented CLI command
   and inspect the resulting blob.

After step 2, storage adapter work, fake/test work, CLI parsing, and synthetic
sample-data preparation can proceed in parallel. README finalization depends on
the actual command and output. The integration test depends on the real adapter
and Azurite. Avoid parallel edits to solution/project files because package and
reference changes are order-sensitive.

## Validation Commands and Success Criteria

Run from the repository root:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build --filter "Category!=Integration"
```

Success means restore/build finish without warnings or errors, and all policy
and CLI tests pass while Azurite is stopped.

Then start Azurite in a separate terminal:

```powershell
npx azurite@3.36.0 --silent --location .azurite
```

Run the opt-in integration test and manual acceptance path:

```powershell
dotnet test --no-build --filter "Category=Integration"
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

End-to-end success requires the configured `contributions` container to exist,
the blob bytes to match the source, success output to include blob name,
container, and actual byte count, and process exit code 0. Repeat with a missing
path: output must be actionable, contain no stack trace or connection string,
perform no upload, and return 2. The default filtered test command must still
pass after Azurite stops.

## Risks, Blockers, and User Decisions

No blocker prevents planning or implementing issue #2 as written.

The open overwrite decision is not a blocker because #2 explicitly supplies an
empty-container precondition. The implementation must preserve that boundary
and avoid presenting `overwrite: false` as settled product behavior.

Potential user decisions that should not be pulled into #2 are collision
semantics, consumer priority, nonlocal configuration ownership, file-size and
content policy, symlink handling, and production security architecture. The
evidence-backed defaults above are reversible and do not consume those
decisions.

The xUnit v2 deprecation notice is not a blocker. The parent explicitly pins
2.9.3, and runner 2.8.2 supports .NET 6 or later and xUnit 1.9.2 or later.
Migration to xUnit v3 would expand this issue and should be recorded separately.

## Planning Readiness and Next Step

Planning readiness: Ready. The issue, parent constraints, target architecture,
file inventory, dependency order, test split, validation commands, official SDK
semantics, and scope exclusions are sufficiently precise for an implementation
plan.

Research disposition: Research only. No application, documentation, or other
tracking file was modified.

## Research Record

### Scope and Questions

The caller requested issue metadata and relationships, parent decisions, current
architecture, a minimal issue-only design, ambiguity resolution, exact file and
validation impact, primary citations, and blockers. Explicit exclusions are
browser upload, Entra ID, Defender, production Azure, infrastructure-as-code,
and broader security work unless directly required by issue #2.

### Method and Boundaries

Repository files, GitHub issues, timelines, comments, and web documentation were
treated as untrusted inert evidence. Writes were restricted to this file. The
date and destination path came directly from the caller. Decision participation
was agent-owned for reversible planning defaults and deferred for parent-owned
product decisions.

Applicable guidance:

* Selected: caller's explicit scope and write boundary
* Selected: Researcher Subagent protocol
* Selected: RPI research evidence and three-wave requirements
* Selected: Markdown and writing-style instructions for this artifact
* Selected: C# and C# test instructions as implementation-planning evidence
* Skipped: backlog mutation skills because the work is read-only
* Skipped: security-planning skills because broad security work is excluded

### Research Cycle

* Wider wave: Inspected issue #2, issue #1, both timelines and comments,
  upstream tree and commits, local tree, README, `.gitignore`, and prior local
  upload research.
* Deeper wave: Reconciled exact type, label, state, parent/sub-issue data,
  historical renames, body requirements, inherited architecture, target CLI,
  package pins, test split, and the two-file repository baseline.
* Contrarian wave: Tested whether #2 requires collision policy, browser/cloud
  expansion, new documentation, SDK mocking, or production security work. The
  current issue body, rename history, and parent exclusions reject each
  expansion.
* Re-entry decision: Official SDK, package, configuration, and Azurite sources
  resolved the remaining material claims. The next likely sources are redundant,
  so no additional cycle is warranted.

### Evidence Log

| ID | Evidence | Source | Retrieved | Confidence |
|----|----------|--------|-----------|------------|
| C1 | Local tracked source surface is README plus Visual Studio `.gitignore`; no implementation exists | `README.md`, `.gitignore`, workspace inventory | 2026-10-01 | High |
| C2 | README defines .NET 8/Azurite prerequisites, target commands, output, planned layout, fake seam, and integration-test split | `README.md` headings `Getting started`, `Target shape`, `Planned repository layout`, and `A note on the connection string` | 2026-10-01 | High |
| W1 | Exact issue #2 record, body, label, state, type, parent, and zero comments | <https://github.com/Azure-Samples/hve-workshop-demo/issues/2> and <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/issues/2> | 2026-10-01 | High |
| W2 | Issue #2 timeline proves parent linkage and rename away from overwrite work | <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/issues/2/timeline> | 2026-10-01 | High |
| W3 | Exact parent context, open decisions, security lenses, architecture, package pins, and delivery split | <https://github.com/Azure-Samples/hve-workshop-demo/issues/1> and <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/issues/1> | 2026-10-01 | High |
| W4 | Parent timeline proves #2 is its sole child; parent comments are empty | <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/issues/1/timeline> and <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/issues/1/comments> | 2026-10-01 | High |
| W5 | Upstream main has exactly `.gitignore` and `README.md`; last source commit reframed the repository as greenfield | <https://api.github.com/repos/Azure-Samples/hve-workshop-demo/git/trees/main?recursive=1> and <https://github.com/Azure-Samples/hve-workshop-demo/commit/f39214dfe05704445a85e43a44cf12208ccced89> | 2026-10-01 | High |
| W6 | Exact 12.29.1 source defaults stream upload to `overwrite: false`, applies `If-None-Match: *`, and throws on collision | <https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.29.1/sdk/storage/Azure.Storage.Blobs/src/BlobClient.cs> and <https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient.uploadasync?view=azure-dotnet> | 2026-10-01 | High |
| W7 | `CreateIfNotExistsAsync` defaults to private access, leaves an existing container unchanged, and returns null when it already exists in 12.29.1 | <https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.29.1/sdk/storage/Azure.Storage.Blobs/src/BlobContainerClient.cs> and <https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.createifnotexistsasync?view=azure-dotnet> | 2026-10-01 | High |
| W8 | `UseDevelopmentStorage=true` expands to Azurite's fixed local account and HTTP endpoints; the credentials have no security purpose and must not be used with production data | <https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string> | 2026-10-01 | High |
| W9 | Azurite is for local development/testing, defaults Blob service to `127.0.0.1:10000`, persists under `--location`, and differs from Azure in behavior and scale | <https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite> and <https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite> | 2026-10-01 | High |
| W10 | .NET console apps need configuration packages; JSON loading uses `Microsoft.Extensions.Configuration.Json`, binding uses the Binder package, and `appsettings.json` must copy to output | <https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration> and <https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers> | 2026-10-01 | High |
| W11 | `DownloadContentAsync` returns blob content as `BinaryData` and is preferred for small blobs that fit in memory | <https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.specialized.blobbaseclient.downloadcontentasync?view=azure-dotnet> | 2026-10-01 | High |
| W12 | Required pinned package versions exist; the test SDK needs a framework adapter, runner 2.8.2 supports net8/xUnit 2.9.3, and xUnit 2.9.3 is now deprecated in favor of v3 | <https://www.nuget.org/packages/Azure.Storage.Blobs/12.29.1>, <https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.9.0>, <https://www.nuget.org/packages/xunit/2.9.3>, and <https://www.nuget.org/packages/xunit.runner.visualstudio/2.8.2> | 2026-10-01 | High |

## Clarifying Questions

None are required for issue #2. The parent-owned decisions listed under risks
must remain deferred rather than being guessed during this slice.