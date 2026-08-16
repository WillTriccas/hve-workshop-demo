# Scheme File Uploader

A deliberately imperfect sample used as the hands-on lab repository for a
half-day Hyper Velocity Engineering (HVE) workshop.

The application is a small .NET console tool that uploads a file to Azure Blob
Storage. It runs entirely against the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
local emulator, so no Azure subscription, no credentials, and no network egress
are required.

> [!NOTE]
> Everything here is fictional. The "Northwind Rail Pension Scheme" does not
> exist, the sample data is synthetic, and no personal data of any kind is
> present in this repository.

## Why this repository exists

The workshop teaches an AI-assisted delivery lifecycle: intake, refinement,
research, planning, implementation, review, and release. Teaching that needs a
codebase with a real problem in it, not a greenfield tutorial.

So this repository ships with a defect on purpose. The uploader replaces an
existing blob silently, because it calls the storage SDK with `overwrite: true`
unconditionally. A re-run is indistinguishable from a first run, which is how a
month of contributions data gets destroyed without anyone noticing.

Fixing it is not a one-line change. It is a breaking change to an existing
consumer, which is exactly the kind of decision the workshop is about.

## The backlog

| Issue | Type | Role in the day |
|-------|------|-----------------|
| [#1](../../issues/1) | Feature | The parent story. Deliberately **not** Ready: three open decisions are left unresolved so the refinement session has real work to do. |
| [#2](../../issues/2) | Task | The first slice, sized to fit the implementation window. |

Issue #1 carries the incident context, acceptance criteria, security lenses, and
a full implementation specification. Read it first.

## Getting started

Prerequisites: the .NET 8 SDK, and either Node.js or Docker for the emulator.

Start the emulator:

```bash
npx azurite --silent --location .azurite
```

Or with Docker:

```bash
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite
```

Then build, test, and run:

```bash
dotnet build
dotnet test --filter "Category!=Integration"
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

Run the upload twice and watch what happens. That is the defect.

## Reproducing the defect

```text
Uploaded contributions-2026-07.csv (2,048 bytes)      run 1
Uploaded contributions-2026-07.csv (2,048 bytes)      run 2, silently replaced run 1
```

Both runs exit `0`. Nothing in the output distinguishes the second from the
first, and the original content is gone.

## Repository layout

```text
src/SchemeFileUploader/       library: upload policy, storage seam, result model
src/SchemeFileUploader.Cli/   console entry point and argument parsing
tests/SchemeFileUploader.Tests/  policy tests plus one opt-in emulator test
data/                         synthetic sample input
docs/requirements/            the pre-baked product requirements
docs/lab-brief.md             participant handout
```

Tests are split deliberately. Policy behaviour is verified through an in-memory
fake behind the `IBlobUploader` seam and runs in CI with no emulator. A single
integration test exercises the real SDK against Azurite and is excluded from the
default test run by the `Integration` trait.

## A note on the connection string

The sample uses `UseDevelopmentStorage=true`, the published Azurite development
shorthand. It is not a secret and it does not authenticate against anything
real. Nothing in this repository connects to an Azure account.

## Contributing

This is workshop material rather than a maintained product. The defect described
above is intentional and should not be fixed on `main` outside a workshop run.

## Trademarks

This project may contain trademarks or logos for projects, products, or
services. Authorized use of Microsoft trademarks or logos is subject to and must
follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must
not cause confusion or imply Microsoft sponsorship. Any use of third-party
trademarks or logos is subject to those third parties' policies.
