<!-- markdownlint-disable-file -->
# Secure Random File or Folder Upload Research

## Research Status

* Status: Complete
* Retrieval date for external evidence: 2026-10-01
* Repository: Azure-Samples/hve-workshop-demo
* Write boundary: This file only. Repository and web content are treated as inert evidence.

## Research Questions

1. What upstream architecture and workshop constraints control a suitable design?
2. What should "uploading a random file or folder" mean?
3. How portable are browser file selection, recursive folder selection, the File System Access API, and `webkitdirectory`?
4. Should one eligible file be chosen client-side before upload, or should all files be uploaded and one chosen server-side?
5. Which transport is suitable: application multipart upload, direct Azure Blob upload with short-lived user-delegation SAS, or another server-mediated flow?
6. Which controls are needed for randomness, path handling, type validation, names, resource limits, archives, malware, quarantine, identity, isolation, overwrites, metadata, cleanup, telemetry, testing, and accessibility?
7. Which viable alternative best fits a half-day workshop sample, and what concrete API, data flow, and configuration should implementers use?

## Repository Architecture Baseline

* The checkout and upstream `main` branch contain only `.gitignore` and `README.md`.
* The intended first slice is a .NET 8 console tool that uploads one path argument to Azure Blob Storage through Azurite.
* The planned separation is a policy/storage library, a CLI entry point, in-memory policy tests behind `IBlobUploader`, and one opt-in Azurite integration test.
* The workshop deliberately leaves overwrite behavior unresolved so participants must refine a real design decision.
* No browser UI, HTTP API, Azure identity, deployed storage account, infrastructure-as-code, or production security control exists upstream.
* Architecture implication: browser file/folder selection requires a new web client and HTTP control plane. It should be framed as an optional follow-on slice so the baseline workshop can remain local, credential-free, and executable without network egress.

## Working Semantics

Interpret the feature as two explicit selection modes:

* "Choose file" selects exactly one file and uploads it after validation.
* "Choose folder" enumerates the selected directory recursively, filters to eligible regular files, uses a cryptographically secure uniform choice to select exactly one, shows that choice for confirmation, and uploads only that file.

"Random folder upload" must not mean uploading a directory object. Azure Blob Storage stores blobs, not browser directory handles. It must also not silently upload every file from a selected folder. Sending all files increases disclosure, storage, malware, and denial-of-service exposure without serving the stated one-random-file outcome.

Random selection is a fairness and unpredictability requirement, not an authorization control. The client should use `crypto.getRandomValues()` with rejection sampling rather than `Math.random()` or direct modulo reduction. For `n` eligible files, draw an unsigned 32-bit value, reject values at or above `floor(2^32 / n) * n`, then use `value % n`. This avoids modulo bias. Tests should inject an `IRandomIndexSource` or JavaScript equivalent so selection can be deterministic under test.

## Findings

### Browser Selection and Progressive Enhancement

* A native `<input type="file">` is the portable and accessible base. It is widely supported, cannot have its local path populated by script, and exposes selected files through `FileList`. Its `accept` attribute is only a picker hint and must not replace server validation.
* A separate `<input type="file" webkitdirectory multiple>` recursively includes the selected directory hierarchy and exposes each file's relative path through `webkitRelativePath`. Despite its historical vendor prefix and non-standard relative-path property, MDN classifies `webkitdirectory` as newly Baseline in 2025 with current Chrome, Edge, Firefox, and Safari support.
* `window.showDirectoryPicker()` is useful only as an enhancement. It is an experimental WICG API, requires HTTPS and transient user activation, and remains unsupported in Firefox and Safari as of the retrieval date. It can recursively iterate directory handles in supporting browsers, but using it as the only path would unnecessarily exclude users.
* Prefer two visible, explicitly labelled controls, "Choose file" and "Choose folder". Keep the native input keyboard reachable. Do not hide it with `display:none` or `visibility:hidden`; those remove it from assistive technology. Announce enumeration, selection, validation, upload progress, success, and failure through a restrained `role="status"` live region; use textual errors associated with the control.
* Folder enumeration must be bounded before random choice. Stop and report a textual error after the configured file-count or enumeration-time limit; do not continue traversing an unexpectedly large tree. Symlink behavior is browser/OS mediated and cannot be trusted as a security boundary.

### Random Choice Placement

Client-side choose-one-before-upload is the recommended semantic. The browser already has the candidate `File` objects, so filtering and random selection can happen without transmitting rejected content or filenames. The server still independently validates the chosen file and all authorization and resource limits.

Uploading all candidates and selecting server-side is viable only when the product requirement is an auditable server-controlled draw over a declared candidate set. It creates a much larger privacy and attack surface, requires batch manifests and cleanup for unselected files, and is a poor fit for the half-day sample.

## Alternatives

| Alternative | Benefits | Costs and risks | Suitable use |
|-------------|----------|-----------------|--------------|
| ASP.NET Core multipart proxy | Simplest trust boundary; authentication, authorization, size enforcement, signature validation, streaming, and storage write remain server-controlled; works when Blob Storage has no public endpoint | Application pays for and scales with every byte; another network hop; request timeouts and buffering mistakes are easier to introduce; large-file progress and retries need design | Best first workshop slice, small files, synchronous inspection, private-endpoint-only storage, or environments that cannot expose the Blob endpoint to browsers |
| Direct browser-to-Blob upload with user-delegation SAS | Removes upload bytes from the application tier; Azure SDK supports browser `Blob`/`File` upload, progress, abort, and block upload; scales better for larger files | Untrusted bytes reach storage before content validation; requires tightly scoped SAS, Blob CORS, public endpoint reachability or an appropriate edge design, asynchronous scan orchestration, and a durable upload record | Best production-oriented extension when files can be quarantined and storage is reachable from the browser |
| Server uploads after streaming request to temporary local storage | Allows deep synchronous inspection before Azure write | Doubles I/O, requires secure temporary storage and cleanup, creates disk-pressure risk, and performs poorly on horizontally scaled ephemeral hosts | Only when a scanner or parser requires seekable local files and cannot inspect a stream or quarantine blob |
| Upload every folder candidate, then choose on the server | Server controls and can audit the draw | Discloses and stores every candidate, multiplies cost and malicious-input exposure, and needs batch cleanup | Only when an auditable server-side draw over an explicitly submitted set is a product requirement |

The recommended production design is the direct-upload alternative implemented as a hybrid control plane/data plane. The ASP.NET Core application authenticates the user, applies policy, allocates one opaque destination, and issues one constrained capability. The browser sends only the selected file directly to a private quarantine container. A trusted backend verifies the committed blob and promotes it only after validation and malware scanning.

For a half-day workshop, implement the multipart proxy first if the teaching objective is policy and storage behavior. It needs fewer Azure resources and preserves the existing `IBlobUploader` seam. Present direct SAS upload as the next slice when the objective includes cloud identity, asynchronous workflows, or scalable transfer. The two approaches can share selection, policy, upload-record, validation, and promotion abstractions.

## Recommended Design

### Trust Boundaries and Components

1. The browser is untrusted. It may suggest a name, media type, size, relative path, ETag, or completion result, but none is authoritative.
2. The ASP.NET Core control plane authenticates with Microsoft Entra ID, derives the tenant and subject from trusted claims, authorizes the upload, rate-limits initiation, creates the upload record, chooses the storage coordinates, and mints the user-delegation SAS.
3. Azure Blob Storage is the untrusted-content landing zone. The `quarantine` container is private and is never a download source for end users.
4. Microsoft Defender for Storage or another approved scanner supplies a trusted scan result. Blob index tags can aid operations but are not the authorization source because callers with tag permission can change them.
5. A promotion worker re-reads the upload record and blob properties, validates the event identity and expected ETag/version, performs remaining content checks, and copies clean content to a private `clean` container under a new server-owned name.
6. The application database is authoritative for ownership, tenant, expected size, original display name, state, scan evidence, hashes, timestamps, and clean blob coordinates. Never authorize a download from a user-supplied blob path or from blob tags alone.

### State Machine

Use compare-and-swap updates, a row version, or equivalent optimistic concurrency. Terminal states are immutable except through a privileged retention process.

```text
Initiated -> Uploading -> Uploaded -> Scanning -> Clean -> Promoting -> Available
		 |            |           |          |          |          |
		 +------------+-----------+----------+----------+----------+-> Expired
															+----------+-----------------------> Rejected
															+----------+-----------------------> ScanFailed
```

* `Initiated`: authorization succeeded and a one-use destination was allocated.
* `Uploading`: optional UI state; the server need not persist it.
* `Uploaded`: the completion endpoint verified an existing committed blob with the expected byte length and captured its ETag/version.
* `Scanning`: a scan is expected or has started.
* `Clean`: the trusted scan result says no threats were found and refers to the expected blob identity.
* `Promoting`: the worker owns a lease or idempotency claim for promotion.
* `Available`: a clean copy exists and only the control plane may issue read access.
* `Rejected`: validation or the scan found a policy violation or malicious content.
* `ScanFailed`: scan error, timeout, unsupported condition, or indeterminate result. It must fail closed.
* `Expired`: the upload was abandoned or exceeded retention.

Do not make content available from `Uploaded`, `Scanning`, or `Clean`. Availability begins only after the promotion operation succeeds and the authoritative record reaches `Available`.

### Storage Naming and Isolation

Use a server-generated identifier such as a UUIDv7 or 128-bit random value. A suitable quarantine path is `tenant-hash/yyyy/mm/upload-id/payload`, where `tenant-hash` is a stable opaque mapping and not a raw customer identifier. The SAS is scoped to this exact blob, not a container or tenant prefix. Preserve the sanitized original filename only as database display metadata; do not use it as a storage path.

For the workshop, one shared quarantine container and one shared clean container are sufficient if every operation is mediated by an upload record and an exact blob-scoped SAS. For stronger commercial isolation, use a container per tenant. Use a storage account per tenant or deployment stamp for regulatory isolation, tenant-controlled keys, independent networking, independent quotas, or noisy-neighbor protection.

### SAS Profile

Issue a blob-scoped user-delegation SAS created with the application's managed identity:

* Resource: the one allocated quarantine block blob.
* Permission: `Create` only. Do not grant `Read`, `Write`, `Delete`, `List`, or `Tags`. Azure documents that `Create` can create a new block blob, while overwriting requires `Write`.
* Transfer mode: the 10 MiB workshop profile uses one `Put Blob` request and therefore stays below the JavaScript SDK's default 256 MiB parallel-upload threshold. For larger files, `Put Block` and `Put Block List` support create-only SAS only with Storage service version `2026-04-06` or later. Pin an SDK and service version that use this behavior and prove it in Azure. Do not silently add `Write` permission as a compatibility fallback.
* Protocol: HTTPS only.
* Lifetime: five minutes by default, bounded by the user-delegation key lifetime. Omit the start time or allow small clock skew rather than setting a future start.
* Correlation: include a GUID SAS correlation ID and log it with upload ID, tenant ID, subject ID, expiry, and policy version. Never log the SAS URI or query string.
* Destination: a cryptographically unguessable blob name generated by the server.
* Optional hardening: evaluate user-bound user-delegation SAS when the selected Storage service version and browser authentication flow support it. It requires the end user to present a matching Entra bearer token and should not block the initial implementation.

The browser should additionally use `If-None-Match: *`. Conditional creation returns `412 Precondition Failed` if the blob already exists. This is defense in depth alongside `Create`-only permission. The completion endpoint must be idempotent and must reject any ETag or version mismatch.

### Validation and Quarantine

Apply validation in layers:

1. Before SAS issuance, accept only configured extensions, media types, and declared sizes; reject empty files, excessive size, disallowed extensions, and users or tenants over quota. These checks save work but do not establish trust.
2. At completion, fetch blob properties using the managed identity. Verify exact account, container, blob name, committed length, block-blob type, ETag/version, and freshness against the upload record. Do not trust values echoed by the browser.
3. In quarantine, detect type from signatures or a vetted parser, reconcile it with the allowed extension, reject polyglots according to policy, compute a SHA-256 content digest, and parse only within strict CPU, memory, depth, and time budgets.
4. Scan every blob. Defender results of malicious, failed, timed out, unsupported, not scanned, or missing are non-clean outcomes. Only a trusted clean result for the expected blob ETag/version can advance the record.
5. If archives are allowed, cap compressed size, uncompressed size, entry count, nesting depth, per-entry size, compression ratio, path length, and total scan time. Reject absolute paths, drive roots, `..` traversal, links, devices, and duplicate normalized names. The safer initial policy is to reject archives.
6. Apply content disarm and reconstruction only for supported document types when the business need justifies it. CDR complements, rather than replaces, malware scanning.
7. Promote by server-side copy to the clean container, verify the resulting copy and hash as required, then mark `Available`. Delete or retain the quarantine source according to incident and evidence policy.

Client-reported `File.type`, extension, relative path, and original name are display or early-filter inputs only. Store the display name encoded as text, strip path components and control characters, normalize Unicode according to an explicit policy, cap length, and set downloads to `Content-Disposition: attachment` with a separately encoded filename. Do not serve active content inline from the application origin.

## Concrete API, Data Flow, and Configuration

### HTTP Contract

All control-plane endpoints require authenticated HTTPS, tenant-aware authorization, request-size limits, rate limiting, anti-forgery protection when cookie authentication is used, and no-store responses where a SAS appears.

`POST /api/uploads`

```json
{
	"originalName": "report.pdf",
	"declaredContentType": "application/pdf",
	"declaredLength": 483921,
	"selectionMode": "folder"
}
```

Successful response, `201 Created`:

```json
{
	"uploadId": "0199...",
	"uploadUrl": "https://account.blob.core.windows.net/quarantine/.../payload?<redacted>",
	"expiresAt": "2026-10-01T12:05:00Z",
	"requiredHeaders": {
		"x-ms-blob-type": "BlockBlob",
		"If-None-Match": "*"
	},
	"maximumBytes": 10485760
}
```

The endpoint validates policy, quotas, and initiation frequency before creating the record. Return a generic `400` for invalid metadata, `403` for unauthorized policy use, `409` for a conflicting idempotency key, and `429` for rate or quota pressure. Accept an `Idempotency-Key` header and bind it to tenant, subject, and normalized request fields.

`POST /api/uploads/{uploadId}/complete`

```json
{
	"etag": "\"0x8D...\""
}
```

The ETag is a hint, not proof. The server authorizes ownership, obtains blob properties directly, checks the expected destination and size, records the authoritative ETag/version, and idempotently transitions to `Uploaded` or `Scanning`. A retry that reports the same committed blob returns `202 Accepted`; a different identity returns `409 Conflict`.

`GET /api/uploads/{uploadId}`

```json
{
	"uploadId": "0199...",
	"state": "Scanning",
	"displayName": "report.pdf",
	"bytes": 483921,
	"failureCode": null
}
```

Return only coarse failure codes safe for the owner, such as `type-not-allowed`, `malware-detected`, `scan-unavailable`, or `expired`. Keep scanner signatures and internal paths in protected telemetry.

`DELETE /api/uploads/{uploadId}`

Cancel only nonterminal uploads owned by the caller. Mark the record expired and queue trusted backend deletion; never give the browser delete permission.

`POST /internal/storage-scan-events`

This is not a public user endpoint. Prefer Event Grid delivery authenticated with Entra ID or another supported trusted mechanism. Validate the event type, subscription, storage account, container, decoded blob URI, upload mapping, sequencer where applicable, and ETag/version before applying an idempotent transition. A forged event, stale event, duplicate event, or event for a different blob must not change availability.

### End-to-End Flow

1. The user chooses file mode or folder mode through a native input.
2. In folder mode, the client bounds enumeration, filters locally by the public portion of policy, chooses one index with Web Crypto rejection sampling, and asks for confirmation. Only the selected file's metadata is sent to the server.
3. The browser calls `POST /api/uploads`. The server derives tenant and user identity from trusted authentication, authorizes, applies quotas, creates the upload record and opaque blob path, obtains or reuses a short-lived user-delegation key, and returns a five-minute create-only blob SAS.
4. The browser uploads the `File` with `BlockBlobClient.uploadData`, progress reporting, an `AbortController`, `If-None-Match: *`, and no client-controlled tags or metadata used for security decisions.
5. On successful commit, the browser sends the returned ETag to the completion endpoint. If this request is lost, the user may retry it; a reconciliation job can also detect initiated records whose exact blob now exists.
6. The server reads authoritative blob properties and advances the record. The blob remains unreadable to the user.
7. Defender or the scanner evaluates the blob. The trusted event handler correlates the result to the upload ID and expected ETag/version.
8. A clean result starts idempotent server-side promotion. All other results remain quarantined and unavailable, with malicious content deleted or retained according to incident policy.
9. After copy verification, the worker sets `Available`. Download requests authorize against the database and either proxy the content or issue a short-lived read-only SAS for the one clean blob.
10. Reconciliation and lifecycle processes expire abandoned records and delete stale quarantine blobs. Lifecycle policy is a backstop, not a real-time scheduler; policy changes can take up to 24 hours to begin and evaluation latency varies.

### Illustrative Server SAS Generation

The exact SDK calls should be checked against the version selected during implementation. The security-relevant shape is:

```csharp
var now = DateTimeOffset.UtcNow;
var delegationKey = await blobServiceClient.GetUserDelegationKeyAsync(
		startsOn: now.AddMinutes(-1),
		expiresOn: now.AddMinutes(10),
		cancellationToken);

var sas = new BlobSasBuilder
{
		BlobContainerName = quarantineContainer,
		BlobName = allocatedBlobName,
		Resource = "b",
		ExpiresOn = now.AddMinutes(5),
		Protocol = SasProtocol.Https,
		CorrelationId = uploadId.ToString()
};
sas.SetPermissions(BlobSasPermissions.Create);

var query = sas.ToSasQueryParameters(
		delegationKey.Value,
		blobServiceClient.AccountName);
```

Use `DefaultAzureCredential` locally with an explicit developer identity and managed identity in Azure. Do not fall back to account keys in production. Cache a user-delegation key only in process, protect it as a credential, keep its lifetime short, and ensure its expiry always exceeds the SAS expiry.

### Illustrative Browser Upload

```javascript
import { BlockBlobClient } from "@azure/storage-blob";

const client = new BlockBlobClient(init.uploadUrl);
const response = await client.uploadData(selectedFile, {
	abortSignal: abortController.signal,
	conditions: { ifNoneMatch: "*" },
	blobHTTPHeaders: { blobContentType: "application/octet-stream" },
	onProgress: ({ loadedBytes }) => reportProgress(loadedBytes, selectedFile.size)
});

await completeUpload(init.uploadId, response.etag);
```

Set stored content type to `application/octet-stream` until trusted detection completes. The SDK's older `uploadBrowserData` method is deprecated; use `uploadData`. Pin and review the package version, and verify that the selected parallel-upload path applies the access condition to the final commit. The create-only SAS remains the storage-enforced overwrite control even if a client omits the condition.

This example is intentionally single-shot because the workshop maximum is 10 MiB. If that maximum grows above the SDK's single-shot threshold, first prove that the selected SDK sends Storage service version `2026-04-06` or later for both block staging and block-list commit. Otherwise the create-only SAS will not authorize the parallel path. Granting `Write` would make overwrite prevention depend on a client-supplied condition and is not the recommended fallback.

### Azure Configuration

* Use a general-purpose v2 storage account with secure transfer required, minimum TLS 1.2 or later, public blob access disabled, and Shared Key authorization disabled where supported.
* Keep `quarantine` and `clean` containers private. Assign the control-plane managed identity `Storage Blob Delegator` at storage-account scope because delegation-key generation is account-scoped, plus only the required data actions at container scope. A custom role can separate create/read-properties/promote/delete duties more narrowly than `Storage Blob Data Contributor`.
* Assign the scanner and promotion worker separate managed identities and least-privilege roles. The browser receives no Azure RBAC assignment.
* Direct browser upload requires the Blob endpoint to be reachable from the browser. A private endpoint alone is normally incompatible with arbitrary Internet clients. If policy forbids public network access, choose the proxy transport or an approved edge/private-access architecture.
* Configure Blob CORS with the exact application origins, `PUT` and `OPTIONS`, only required request headers, only required exposed response headers such as `ETag` and request IDs, and a bounded preflight cache. CORS is browser policy, not authorization.
* Enable Defender for Storage malware scanning on upload where its limits and regional availability fit. Its documented maximum supported blob size is 50 GB, so the application maximum must be no larger and will normally be much smaller.
* Route scan events through Event Grid to a trusted worker or internal endpoint. Configure retries, dead-lettering, alerting, and reconciliation for missing or delayed events.
* Enable diagnostic settings for control-plane logs, Blob read/write/delete logs, Defender alerts/results, Event Grid delivery failures, application traces, and metrics. Redact SAS query strings and original names where telemetry policy treats them as personal data.
* Add a lifecycle rule that deletes stale blobs under the quarantine prefix after the chosen retention period. Use database-driven cleanup for precise expiry. Remember that soft delete retains lifecycle-deleted blobs for its configured retention period and may conflict with rapid eradication requirements.
* Use separate storage accounts for production and nonproduction. Azurite remains useful for local transport and overwrite tests but cannot validate managed identity, user-delegation SAS, Defender, Event Grid, Azure networking, or production CORS behavior.

### Suggested Defaults for the Workshop

| Setting | Initial value | Rationale |
|---------|---------------|-----------|
| Maximum selected file size | 10 MiB | Keeps proxy and direct variants easy to exercise; below platform and scanner limits |
| Maximum folder entries enumerated | 1,000 | Bounds browser work and accidental disclosure |
| Folder enumeration time | 5 seconds | Prevents an apparently hung picker workflow |
| Allowed types | A small explicit set such as `.txt`, `.png`, and `.pdf` | Demonstrates extension plus signature policy without pretending to support arbitrary files |
| Archives | Rejected | Avoids decompression and recursive-parser risk in the first slice |
| SAS lifetime | 5 minutes | Limits capability exposure while allowing a 10 MiB upload |
| Upload record expiry | 15 minutes | Allows retries but closes abandoned sessions quickly |
| Scan timeout | Product SLO, for example 15 minutes | Converts missing results into a defined fail-closed state |
| Quarantine retention | 1 day for abandoned benign data; security policy for malicious samples | Supports reconciliation while bounding cost and exposure |

## Security Control Matrix

| Threat or failure | Preventive control | Detection and recovery |
|-------------------|--------------------|------------------------|
| Cross-tenant upload or read | Derive tenant from trusted claims; authorize every upload record; exact blob-scoped SAS; opaque tenant mapping; private containers | Audit tenant, subject, upload ID, correlation ID, and storage request ID; negative tenant-isolation tests |
| SAS theft or replay | HTTPS; five-minute expiry; create-only permission; one opaque blob; no SAS in logs, history, analytics, or referrers; `Cache-Control: no-store` | Alert on use after record terminal state or from anomalous source; revoke delegation keys for a broad emergency response, noting revocation can be delayed |
| Overwrite or race | Server-generated unique path; `Create` only; `If-None-Match: *`; idempotency key; ETag/version correlation | Treat `412` as conflict; alert on unexpected existing destination; never silently overwrite |
| Malicious or active content | Allowlist; signature/parser validation; quarantine; malware scan; optional CDR; force attachment and separate download origin | Fail closed; record scanner evidence; isolate or delete per incident policy |
| Filename or path traversal | Ignore client path for storage; generated names; strip path components; normalize and encode display names | Unit tests for separators, Unicode, controls, reserved names, and archive traversal |
| Oversized upload or resource exhaustion | Declared-size gate; storage and application maximum; folder count/time bound; tenant quotas; rate limiting; parser budgets; archive limits | Metrics for bytes, starts, completions, throttles, timeouts, and abandoned records; cleanup and backpressure |
| Type spoofing or polyglot | Treat extension and MIME as hints; inspect signatures and parse with vetted libraries; reconcile type and extension | Record detected type and policy decision; regression corpus for malformed and polyglot files |
| Forged completion or scan event | Server reads blob properties; authenticated internal event path; exact account/container/name/ETag matching; state-machine concurrency | Reject and log stale, duplicate, mismatched, and out-of-order events; scheduled reconciliation |
| Partial or uncommitted block upload | SDK final commit; completion checks committed blob properties; expiration; no availability before promotion | Reconciler expires records; Azure garbage-collects uncommitted blocks, with lifecycle/database cleanup for committed abandoned blobs |
| Tag or metadata tampering | SAS excludes tag and write permissions; database is authoritative; backend sets trusted metadata after scan | Compare authoritative record with storage properties; never authorize from tags |
| Denial of wallet or noisy neighbor | Per-user and per-tenant initiation/byte quotas; concurrency limits; shared-account capacity planning; optional tenant containers/accounts | Cost, transaction, capacity, throttling, and Defender scan-volume alerts; tenant-level application metering |
| Stale quarantine data | Short upload expiry; reconciliation; lifecycle backstop; explicit malicious-sample policy | Monitor oldest nonterminal record and oldest quarantine blob; alert on cleanup failures |
| CSRF or unauthorized initiation | Bearer-token API or anti-forgery for cookie auth; same-site cookies; authorization policy | Security tests for missing/invalid token and cross-origin form requests |
| Supply-chain compromise | Pin NuGet/npm versions; dependency review and scanning; minimal libraries; trusted parsers | SBOM, update cadence, vulnerability alerts, and repeatable builds |

Logging must avoid SAS values, authorization headers, file content, raw original paths, and unnecessary personal data. Record policy version, outcome codes, byte counts, timings, upload ID, pseudonymous tenant/user identifiers, ETag/version, scanner engine/result identifiers, and Azure request/correlation IDs. Protect logs against tenant access and define retention separately from uploaded content.

## Tests and Accessibility

### Unit and Property Tests

* Random selection returns an index in `[0, n)`, rejects draws in the biased tail, retries correctly, handles `n = 1`, rejects `n <= 0`, and is approximately uniform in a non-gating statistical diagnostic. Deterministic injected draws test the exact rejection boundary.
* Folder filtering applies the same public policy regardless of enumeration order and never transmits rejected candidates. Count and time bounds stop enumeration with a stable error.
* Name handling strips both slash styles, rejects control characters and empty results, normalizes according to policy, caps length, and safely formats `Content-Disposition`.
* Upload authorization denies another tenant or subject, expired records, disallowed types, empty or oversized files, exceeded quotas, and replayed idempotency keys with changed content.
* State transitions reject impossible, stale, duplicate, and out-of-order events. Duplicate completion, duplicate clean events, and duplicate promotion are idempotent.
* Event validation covers wrong account, container, blob, tenant, upload ID, ETag, version, event type, and unauthenticated source.
* Archive-policy tests use zip-slip paths, absolute paths, nested archives, duplicate normalized names, link entries, huge ratios, too many entries, and malformed central directories. If archives remain disabled, verify every archive signature is rejected even when renamed.

### Integration and Security Tests

* With Azurite, verify upload success, exact-name collision failure, `If-None-Match` behavior where supported, streaming limits, cleanup, and `IBlobUploader` error mapping.
* Against a disposable Azure account, verify managed identity, user-delegation SAS generation, `Create`-only upload success, overwrite denial, expiry, HTTPS-only enforcement, exact blob scope, CORS allowed/disallowed origins, Shared Key denial, private containers, and diagnostic correlation.
* Verify a browser cannot read, list, tag, delete, or write another blob with the issued SAS.
* Verify completion cannot make content available, clean scan for the wrong ETag cannot promote, malicious and indeterminate results never promote, and scan timeout reaches `ScanFailed`.
* Exercise Event Grid duplicate delivery, delay, dead-letter, worker restart, copy retry, and reconciliation after each process boundary.
* Load-test initiation rate, concurrent direct uploads, completion calls, scan-event bursts, and tenant quota contention. Validate storage throttling and application backpressure behavior.
* Use an approved inert antimalware test file in an isolated nonproduction account to validate end-to-end quarantine. Never use live malware in a workshop environment.

### Accessible Interaction

* Provide separate native file and folder controls with persistent visible labels and instructions. Do not encode mode or errors by color alone.
* After folder enumeration, place keyboard focus on a confirmation region or heading only when this is a user-initiated context change; otherwise preserve focus and announce the randomly selected display name in a polite status region.
* Require explicit confirmation before upload so the user knows which file was selected. Provide a reroll action only if product semantics permit it, and label the action plainly.
* Use a native `<progress>` element with an associated text label when total bytes are known. Announce milestone changes rather than every progress event to avoid excessive screen-reader output.
* Keep cancellation keyboard operable and ensure it aborts network work, updates status, and leaves the server record eligible for cleanup.
* Associate validation errors with the relevant control, summarize errors at the start of a failed submission, and move focus only for a deliberate validation result.
* Test with keyboard only, 200 percent zoom, high contrast or forced colors, reduced motion, and representative screen readers. Test the `webkitdirectory` fallback in each supported browser and ensure unsupported directory selection is explained without blocking file mode.

## Unresolved Gaps

The following are product or deployment decisions, not missing architecture mechanics:

* Choose the allowed type set, maximum size, and whether archives or CDR are required from actual business need and threat tolerance.
* Decide whether users may reroll the random choice. Unlimited rerolls change the meaning from random selection to user selection and should be explicit.
* Define whether the draw must be auditable or reproducible. Client-side Web Crypto provides fair selection but the server cannot prove the complete candidate set. An auditable draw requires a consented manifest or server-side candidates and materially changes privacy and architecture.
* Set tenant isolation from regulatory, geographic, encryption-key, backup, deletion, and noisy-neighbor requirements. The workshop assumption of shared containers is not a universal production recommendation.
* Define malicious-file retention with security and privacy owners. Immediate deletion reduces exposure; retained samples can aid incident response but require stricter access, retention, and legal controls.
* Confirm Defender for Storage availability, price, latency, limits, and result-delivery design in the target Azure regions and subscription. Provide an approved scanner abstraction if Defender is unavailable.
* Decide whether direct browser access to the Blob public endpoint is permitted. If it is not, select the proxy architecture before implementation.
* Confirm the exact Entra claims and tenant-resolution model. Never accept tenant identity from request JSON.
* Define download behavior, retention, data-subject deletion, legal hold, backup, disaster recovery, and content ownership. Upload security is incomplete without the content lifecycle.

## Recommended Next Research

No further research is required to start a design review or implementation plan. Before production implementation:

1. Threat-model the chosen deployment, including identity provider, network topology, scanner event path, download path, and incident handling.
2. Run a small Azure spike that proves create-only user-delegation SAS with the selected .NET and JavaScript SDK versions, parallel upload conditions, CORS, Defender event correlation, and promotion idempotency.
3. Validate the allowed file-type corpus with the selected signature/parser library and document parser-specific resource limits.
4. Cost-model Blob transactions, Defender scans, logs, Event Grid, retained quarantine data, and tenant growth.
5. Conduct privacy, accessibility, and security review for the actual business use, retention policy, and supported browsers.

## Evidence Log

| ID | Evidence or claim | Source | Retrieved | Confidence |
|----|-------------------|--------|-----------|------------|
| R1 | Upstream is a greenfield, two-file repository for a .NET 8 console/Azurite workshop; the intended seams and tests are described but not implemented. | https://github.com/Azure-Samples/hve-workshop-demo and https://api.github.com/repos/Azure-Samples/hve-workshop-demo/contents | 2026-10-01 | High |
| R2 | The local checkout matches the upstream two-file source surface, excluding gitignored research artifacts. | README.md and workspace file inventory | 2026-10-01 | High |
| W1 | Native file inputs are widely supported; `accept` is only a hint and requires server-side validation. | https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/file | 2026-10-01 | High |
| W2 | `webkitdirectory` recursively returns all files with relative paths and is Baseline 2025 across current major browsers, despite the historical prefix. | https://developer.mozilla.org/en-US/docs/Web/API/HTMLInputElement/webkitdirectory and https://developer.mozilla.org/en-US/docs/Web/API/File/webkitRelativePath | 2026-10-01 | High |
| W3 | `showDirectoryPicker()` is experimental, secure-context and user-activation gated, and unsupported by current Firefox and Safari. | https://developer.mozilla.org/en-US/docs/Web/API/Window/showDirectoryPicker and https://wicg.github.io/file-system-access/ | 2026-10-01 | High |
| W4 | Web Crypto specifies cryptographically strong bytes for `getRandomValues()` and a 65,536-byte request ceiling. | https://w3c.github.io/webcrypto/#Crypto-method-getRandomValues and https://developer.mozilla.org/en-US/docs/Web/API/Crypto/getRandomValues | 2026-10-01 | High |
| W5 | Accessible forms need associated labels, textual error identification, and programmatically determinable progress/status updates that do not steal focus. | https://www.w3.org/WAI/tutorials/forms/labels/, https://www.w3.org/WAI/WCAG22/Understanding/error-identification.html, and https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html | 2026-10-01 | High |
| A1 | Microsoft recommends user-delegation SAS over account-key SAS; SAS should be scoped, least-privilege, HTTPS-only, short-lived, protected as a credential, and correlated through application audit logs. | https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview and https://learn.microsoft.com/en-us/rest/api/storageservices/create-user-delegation-sas | 2026-10-01 | High |
| A2 | A user-delegation SAS is constrained by both the issuer's RBAC and its signed permissions; delegation-key generation is account-scoped, while blob data roles can be container-scoped. | https://learn.microsoft.com/en-us/rest/api/storageservices/create-user-delegation-sas and https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-access-azure-active-directory | 2026-10-01 | High |
| A3 | `Put Blob` distinguishes create from overwrite: `Create` or `Write` can create a new block blob, but overwrite requires `Write`. `If-None-Match: *` limits a write to a nonexistent blob and failure returns `412`. `Put Block` and `Put Block List` support create-only SAS for new blobs with service version `2026-04-06` or later. | https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob, https://learn.microsoft.com/en-us/rest/api/storageservices/put-block, https://learn.microsoft.com/en-us/rest/api/storageservices/put-block-list, and https://learn.microsoft.com/en-us/rest/api/storageservices/specifying-conditional-headers-for-blob-service-operations | 2026-10-01 | High |
| A4 | Blob containers should be private; anonymous and Shared Key access can be disabled; Entra ID, managed identity, narrow RBAC scope, secure transfer, firewall/private endpoint choices, and logging provide the recommended storage posture. | https://learn.microsoft.com/en-us/azure/storage/blobs/anonymous-read-access-prevent, https://learn.microsoft.com/en-us/azure/storage/common/shared-key-authorization-prevent, https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-access-azure-active-directory, and https://learn.microsoft.com/en-us/azure/storage/common/storage-network-security | 2026-10-01 | High |
| A5 | Blob Storage CORS controls browser cross-origin requests but is not authorization; direct client access improves transfer scalability but requires constrained SAS and endpoint reachability. | https://learn.microsoft.com/en-us/rest/api/storageservices/cross-origin-resource-sharing--cors--support-for-the-azure-storage-services and https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/storage | 2026-10-01 | High |
| A6 | Azure Storage JavaScript supports browser `Blob`/`File` upload through `uploadData`, parallel transfer options, and conditions; the older `uploadBrowserData` API is deprecated. Concurrent writes to one blob require application control. | https://learn.microsoft.com/en-us/javascript/api/@azure/storage-blob/blockblobclient and https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload-javascript | 2026-10-01 | High |
| A7 | Defender for Storage on-upload malware scanning supports event-driven response and a DMZ/quarantine pattern, has a 50 GB supported blob limit, and can report non-success outcomes that must not be treated as clean. | https://learn.microsoft.com/en-us/azure/defender-for-cloud/on-upload-malware-scanning and https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-for-storage-malware-scan | 2026-10-01 | High |
| A8 | Lifecycle policies can filter by prefix and blob index tags and delete by age, but changes can take up to 24 hours to begin, execution is periodic, and soft delete retains deleted blobs. | https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview and https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-policy-delete | 2026-10-01 | High |
| A9 | Azure multitenant guidance describes shared paths, per-tenant containers, and per-tenant accounts as increasing isolation levels, and recommends Valet Key/user-delegation SAS for exact client access plus tenant-aware monitoring and lifecycle design. | https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/service/storage and https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/approaches/storage-data | 2026-10-01 | High |
| S1 | OWASP recommends extension allowlists, generated storage names, size limits, authorization, CSRF protection, storage outside the webroot, signature validation, malware scanning, CDR where applicable, and archive controls. | https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html | 2026-10-01 | High |
| S2 | ASP.NET Core guidance warns against trusting `IFormFile.FileName`, recommends generated storage names, server-side extension/signature checks, size limits, safe staging, malware scanning, and streaming for large uploads. | https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads | 2026-10-01 | High |

## Clarifying Questions Requiring User Input

None. Decisions that require product, security, privacy, networking, or compliance ownership are listed under `Unresolved Gaps` and do not block an implementation spike.
