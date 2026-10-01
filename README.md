---
title: Scheme File Uploader
description: Local .NET 8 and Azurite walking skeleton for the HVE workshop
---

## Workshop context

This repository supports a half-day Hyper Velocity Engineering (HVE) workshop.
The walking skeleton is a .NET 8 command-line tool that uploads one local file
to Azure Blob Storage through the local Azurite emulator.

The workshop covers an AI-assisted delivery lifecycle: intake, refinement,
research, planning, implementation, review, and release. The upload is small on
purpose. It leaves enough room to examine collision policy, configuration,
security boundaries, test design, and evidence.

> [!NOTE]
> Everything here is fictional. The Northwind Rail Pension Scheme does not
> exist, the sample data is synthetic, and no personal data is present in this
> repository.

## Backlog

* [#1](../../issues/1), Feature: parent single-file upload requirement and future roadmap
* [#2](../../issues/2), Task: local single-file and Azurite walking skeleton

Issue #1 carries the broader context, acceptance criteria, and security lenses.
The current implementation is the bounded local slice from issue #2.

## Prerequisites

* .NET SDK 8.0.406, selected by `global.json`
* Docker capable of running Linux containers

No Azure subscription or cloud credentials are required. The application uses
only local Azurite and does not connect to an Azure account.

## Start Azurite

Start the pinned Azurite 3.36.0 container in a separate terminal:

```bash
docker run --rm --name scheme-file-uploader-azurite -p 10000:10000 mcr.microsoft.com/azure-storage/azurite:3.36.0
```

Stop the emulator with `Control+C` in that terminal. The container is removed
when it stops.

## Build and run

Restore and build from the repository root:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

Upload the synthetic workshop file to the default `contributions` container:

```bash
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

Override the destination container for one invocation:

```bash
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv --container workshop-contributions
```

The CLI reads defaults from `src/SchemeFileUploader.Cli/appsettings.json`, then
applies `Uploader__*` environment variables. The `--container` argument has the
highest precedence for the container name.

```bash
Uploader__ContainerName=environment-contributions \
  dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

The command accepts exactly one local file. It rejects directories, symbolic
links, missing files, and files larger than the configured limit. The interim
default limit is 100 MiB.

## Outcomes and create-only behavior

Uploads use an atomic create-only condition. A blob that already has the same
name is never overwritten. Rename the local file or remove the existing test
blob before retrying.

* Exit code `0`: the blob was created and the reported byte count is the source file length
* Exit code `1`: the upload was refused because a blob with the same name already exists
* Exit code `2`: usage, configuration, source-file, emulator, or expected storage failure

Expected output is sanitized. It does not include connection strings, account
keys, SAS queries, absolute source paths, or stack traces.

## Tests

The default suite uses an in-memory `IBlobUploader` fake. It must pass without
Azurite and excludes integration tests explicitly:

```bash
dotnet test --configuration Release --filter "Category!=Integration"
```

With Azurite 3.36.0 running on port 10000, run the opt-in real-SDK tests:

```bash
dotnet test --configuration Release --filter "Category=Integration"
```

The integration category creates unique test-owned containers, verifies exact
uploaded bytes and metadata, proves repeated-name refusal, races concurrent
same-name creates, and deletes only its own containers.

## Synthetic workshop data

`data/contributions-2026-07.csv` is a synthetic, PII-free acceptance fixture.
It exists only to exercise the local upload path and must not be replaced with
customer, employee, pension-scheme, or other personal data.

## Scope and security boundary

Azurite validates the local Blob SDK path and create-only behavior. It does not
validate Azure identity or hosted security controls.

The following capabilities are follow-on work and are not implemented or proven
by this walking skeleton:

* Microsoft Entra authentication and managed identities
* Azure RBAC and production authorization boundaries
* SAS issuance, expiry, revocation, and user delegation
* Browser-based folder selection, recursive folder upload, and CORS
* Quarantine, promotion, retention, and lifecycle workflows
* Malware scanning, Microsoft Defender for Storage, and scan-result handling
* Hosted networking, quotas, monitoring, incident response, and other security controls

Passing the Azurite tests is local integration evidence. It is not evidence that
the application is ready to receive arbitrary or untrusted files in Azure.

## Repository layout

```text
src/SchemeFileUploader/          upload policy, storage boundary, and Blob SDK adapter
src/SchemeFileUploader.Cli/      arguments, configuration, output, and exit codes
tests/SchemeFileUploader.Tests/  fake-backed tests and opt-in Azurite tests
data/                            synthetic workshop input
```

## Contributing

This repository is workshop material rather than a maintained product. Keep
changes aligned with the active workshop branch and its accepted scope.

## Trademarks

This project may contain trademarks or logos for projects, products, or
services. Authorized use of Microsoft trademarks or logos is subject to and must
follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must
not cause confusion or imply Microsoft sponsorship. Any use of third-party
trademarks or logos is subject to those third parties' policies.
