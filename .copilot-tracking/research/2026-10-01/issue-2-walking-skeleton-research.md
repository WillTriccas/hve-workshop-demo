<!-- markdownlint-disable-file -->
# Issue 2 Walking Skeleton Research

## Scope

Plan issue #2, "Build the walking skeleton: upload a file to a container," as
the first local-only implementation slice of issue #1. The scope is limited to
.NET 8, Azurite, a console command, the storage seam, focused tests, synthetic
sample data, and acceptance-path documentation.

## Verified Findings

* The repository contains only `README.md` and `.gitignore`; no implementation
  exists.
* Issue #2 requires a solution, library, CLI, and xUnit test project targeting
  `net8.0`.
* The command accepts `<file>` and optional `--container <name>`, binds Azurite
  settings from `appsettings.json`, creates a missing container, uploads one
  blob, and reports a sanitized result.
* Success returns exit code 0. All failures in this slice return 2. Exit code 1
  remains reserved for the unresolved collision policy.
* `IBlobUploader` must expose `ExistsAsync` and `UploadAsync`, but the service
  must not use `ExistsAsync` to invent an existing-blob policy in this slice.
* Passing `overwrite: false` to Azure Storage is a non-destructive transport
  guard under the empty-container assumption, not a settled product policy.
* Required tests cover fake-backed success and byte count, missing source,
  connection-string redaction, CLI parsing, and one opt-in Azurite round trip.
* The default non-integration test run must pass while Azurite is stopped.
* Browser upload, Entra authentication, production Azure, malware scanning,
  infrastructure as code, retry policy, chunking, and lifecycle controls are
  outside issue #2.

## Selected Implementation Direction

Create the parent-prescribed three-project architecture. Keep policy and result
types in the library, explicit argument and configuration handling in the CLI,
and a hand-written fake in tests. Use the pinned package versions from issue #1
and create a small synthetic CSV fixture. Update `README.md` only where the
walking-skeleton acceptance path makes its greenfield wording obsolete, and add
`.azurite/` to `.gitignore` because the documented emulator command persists
state there.

## Ambiguity Resolution

* Existing blob: do not overwrite and do not add `AlreadyExists`; report a
  sanitized exit-2 failure until issue #1 chooses refuse, replace, or version.
* Consumer: keep the command non-interactive with concise prose and stable 0/2
  exit codes; defer structured output and prompts.
* Configuration: support the local Azurite connection and container only;
  defer cloud environment and credential ownership.
* File security: validate that the path names a readable file and use its base
  name as the blob name; defer content, size, symlink, and malware policies.

## Evidence

* `.copilot-tracking/research/subagents/2026-10-01/issue-2-walking-skeleton-research.md`
  contains the complete issue record, parent analysis, SDK evidence, package
  versions, file inventory, dependency graph, and validation commands.
* `README.md` defines the intended .NET 8/Azurite command, repository layout,
  fake-backed policy tests, and opt-in emulator test.
* GitHub issue #2: <https://github.com/Azure-Samples/hve-workshop-demo/issues/2>
* GitHub issue #1: <https://github.com/Azure-Samples/hve-workshop-demo/issues/1>

## Planning Readiness

Status: Ready. No user decision blocks issue #2. Parent-owned product and
production-security decisions remain intentionally deferred.

## Phase 7 Implementation Compatibility Addendum

### Research Questions

* Which service versions does `Azure.Storage.Blobs` 12.29.1 expose, including
  its default request version?
* Which Blob API versions does Azurite 3.36.0 accept?
* Can `BlobContainerClient` use an explicit `BlobClientOptions.ServiceVersion`
  when constructed from the existing connection string and container name?
* Which of explicit client options, `--skipApiVersionCheck`, changing the SDK
  pin, or changing the Azurite pin is the smallest fix that preserves approved
  pins and upload semantics?

### Compatibility Findings

Status: Complete.

The blocker is a strict request-version mismatch, not an upload-policy or
authentication defect. The restored `Azure.Storage.Blobs` 12.29.1 package
constructs `BlobClientOptions` with `V2026_06_06` by default and therefore sends
`x-ms-version: 2026-06-06`. Azurite 3.36.0 declares a Blob baseline of
`2025-11-05`; its middleware rejects a supplied version unless it occurs in the
release's `ValidAPIVersions` array. `2026-06-06` is absent, so the strict default
returns HTTP 400 `InvalidHeaderValue` before the request reaches the operation
handler.

`Azure.Storage.Blobs` 12.29.1 exposes these `BlobClientOptions.ServiceVersion`
members:

* `V2019_02_02`, `V2019_07_07`, `V2019_12_12`
* `V2020_02_10`, `V2020_04_08`, `V2020_06_12`, `V2020_08_04`,
  `V2020_10_02`, `V2020_12_06`
* `V2021_02_12`, `V2021_04_10`, `V2021_06_08`, `V2021_08_06`,
  `V2021_10_04`, `V2021_12_02`
* `V2022_11_02`
* `V2023_01_03`, `V2023_05_03`, `V2023_08_03`, `V2023_11_03`
* `V2024_02_04`, `V2024_05_04`, `V2024_08_04`, `V2024_11_04`
* `V2025_01_05`, `V2025_05_05`, `V2025_07_05`, `V2025_11_05`
* `V2026_02_06`, `V2026_04_06`, `V2026_06_06`

The exact Azurite 3.36.0 Blob `ValidAPIVersions` list is:

* `2025-11-05`, `2025-07-05`, `2025-05-05`, `2025-01-05`
* `2024-11-04`, `2024-08-04`, `2024-05-04`, `2024-02-04`
* `2023-11-03`, `2023-08-03`, `2023-01-03`
* `2022-11-02`
* `2021-12-02`, `2021-10-04`, `2021-08-06`, `2021-06-08`,
  `2021-04-10`, `2021-02-12`
* `2020-12-06`, `2020-10-02`, `2020-08-04`, `2020-06-12`,
  `2020-04-08`, `2020-02-10`
* `2019-12-12`, `2019-10-10`, `2019-07-07`, `2019-02-02`
* `2018-11-09`, `2018-03-28`, `2017-11-09`, `2017-07-29`,
  `2017-04-17`, `2016-05-31`, `2015-12-11`, `2015-07-08`,
  `2015-04-05`, `2015-02-21`, `2014-02-14`, `2013-08-15`,
  `2012-02-12`, `2011-08-18`, `2009-09-19`, `2009-07-17`, and
  `2009-04-14`

The highest version common to both exact pins is therefore `2025-11-05`.
`2023-05-03` is an exposed SDK version but is not accepted by Azurite 3.36.0,
so selecting an arbitrary older SDK member is not sufficient.

The exact 12.29.1 source and local package XML both expose
`BlobContainerClient(string connectionString, string blobContainerName,
BlobClientOptions options)`. The constructor parses the same connection string,
builds the same endpoint and Shared Key pipeline, and places `options.Version`
in the shared client configuration. `GetBlobClient` passes that configuration
to the child `BlobClient`. Explicit options therefore fit the current adapter
design without changing `UploaderOptions`, `IBlobUploader`, CLI configuration,
authentication, endpoint selection, container naming, or result contracts.

The implementation must cover every directly constructed SDK client used in
Phase 7. `AzureBlobUploader` creates container clients in both `ExistsAsync` and
`UploadAsync`. The integration test independently creates a container client
for download and cleanup. If only the production adapter is pinned, the test's
download or cleanup client still defaults to `2026-06-06` and can fail against
Azurite 3.36.0.

Option comparison:

| Option | Pins preserved | Version validation | Semantic and design effect | Assessment |
|--------|----------------|--------------------|----------------------------|------------|
| Explicit `BlobClientOptions.ServiceVersion.V2025_11_05` | Yes | Preserved | Changes only the REST version header for clients created by this slice; existing connection string, credentials, container creation, private access, non-overwrite guard, upload, download, and cleanup calls remain | Recommended |
| Azurite `--skipApiVersionCheck` | Yes | Disabled globally for that emulator process | Allows any advertised request version even when the emulator implements a lower baseline; startup documentation and every invocation must remain synchronized | Useful diagnostic or temporary escape hatch, not the minimal durable fix |
| Change `Azure.Storage.Blobs` pin | No | Preserved if downgraded far enough | Violates the approved 12.29.1 pin; 12.28.0 and 12.27.0 still default above Azurite 3.36.0, while 12.26.0 defaults to `2025-11-05` | Reject |
| Change Azurite pin to 3.37.0 | No | Preserved | 3.37.0 supports `2026-06-06`, but violates the approved 3.36.0 pin and raises its documented Node.js runtime baseline from 21+ to 22+ | Reject |

### Compatibility Recommendation

Use `new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05)` for
all `BlobContainerClient` instances participating in this slice. Pass it to the
three-argument connection-string constructor in `AzureBlobUploader`, and use the
same explicit service version for the integration test's independent client.

This is the smallest durable fix because it preserves both approved dependency
pins, retains Azurite's strict compatibility check, selects the newest API
version both pinned components explicitly support, and does not widen the
product configuration surface. The exercised operations (`Create Container`,
`Put Blob`, `Get Blob`, `Delete Container`, and existence checks) are in Azurite
3.36.0's support matrix and do not depend on a `2026-06-06` feature. The SDK's
`overwrite: false` behavior and request conditions are selected by the same
12.29.1 upload implementation, so the non-overwrite semantics remain intact.

Prefer one private compatibility constant or private client-options factory in
the storage adapter, rather than adding a public option. The integration test
can construct matching options locally. Do not use `--skipApiVersionCheck` as
the normal workshop command: it hides future SDK/emulator drift and validates a
request version that Azurite 3.36.0 does not claim to implement.

### Compatibility Evidence

* src/SchemeFileUploader/SchemeFileUploader.csproj pins
  `Azure.Storage.Blobs` 12.29.1.
* src/SchemeFileUploader/AzureBlobUploader.cs constructs
  `BlobContainerClient` from the configured connection string and container in
  both storage methods; upload remains `overwrite: false` at the call boundary.
* tests/SchemeFileUploader.Tests/AzuriteIntegrationTests.cs independently
  constructs a default-version client for download and cleanup.
* Local restored package metadata at
  `%USERPROFILE%/.nuget/packages/azure.storage.blobs/12.29.1/lib/net8.0/Azure.Storage.Blobs.xml`
  lists all 31 service-version members and the three-argument
  connection-string constructor. The local DLL reports assembly package version
  12.29.1 and file version 12.2900.126.32304.
* Exact 12.29.1 `BlobClientOptions` source defines the enum through
  `V2026_06_06` and defaults its constructor to `LatestVersion`:
  <https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.29.1/sdk/storage/Azure.Storage.Blobs/src/BlobClientOptions.cs>
* Exact 12.29.1 shared version source maps `LatestVersion`, `MaxVersion`, and
  the request string to `V2026_06_06` / `2026-06-06`:
  <https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.29.1/sdk/storage/Azure.Storage.Common/src/Shared/StorageVersionExtensions.cs>
* Exact 12.29.1 container source accepts connection string, container name,
  and options, stores `options.Version`, and propagates the same configuration
  from `GetBlobClient`:
  <https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.29.1/sdk/storage/Azure.Storage.Blobs/src/BlobContainerClient.cs>
* Exact Azurite 3.36.0 constants identify version 3.36.0, baseline
  `2025-11-05`, and the complete accepted Blob API-version array:
  <https://github.com/Azure/Azurite/blob/v3.36.0/src/blob/utils/constants.ts>
* Exact Azurite 3.36.0 middleware checks `x-ms-version` against that array
  unless version checking is skipped:
  <https://github.com/Azure/Azurite/blob/v3.36.0/src/blob/middlewares/blobStorageContext.middleware.ts>
* Exact Azurite 3.36.0 CLI source documents `--skipApiVersionCheck` as allowing
  all API versions and defaults the check to enabled:
  <https://github.com/Azure/Azurite/blob/v3.36.0/src/common/Environment.ts>
* Azurite 3.36.0 documentation identifies `2025-11-05` as its API baseline,
  documents the strict higher-version rejection strategy, and lists the Blob
  operations used by the walking skeleton:
  <https://github.com/Azure/Azurite/blob/v3.36.0/README.md>
* Microsoft Learn states that `x-ms-version` selects protocol behavior and
  recommends an earlier REST version for `InvalidHeaderValue` version errors:
  <https://learn.microsoft.com/en-us/rest/api/storageservices/versioning-for-the-azure-storage-services>
* Azurite 3.37.0 source and documentation add `2026-06-06` support but require
  Node.js 22+, confirming that an emulator upgrade is a larger dependency and
  prerequisite change:
  <https://github.com/Azure/Azurite/blob/v3.37.0/src/blob/utils/constants.ts>
  and <https://github.com/Azure/Azurite/blob/v3.37.0/README.md>

### Remaining Compatibility Uncertainties

* No material research uncertainty remains for selecting the fix.
* Implementation validation still needs to prove the integration round trip,
  manual CLI upload, and collision path against an actual Azurite 3.36.0 process
  after every participating client uses `V2025_11_05`.
* The chosen version is intentionally an internal compatibility detail for the
  current local-only slice. A later production-Azure story should decide whether
  service-version selection remains fixed, becomes environment-specific, or
  returns to the SDK default after the emulator pin advances.