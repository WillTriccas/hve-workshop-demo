# Scheme File Uploader

Lab repository for a half-day Hyper Velocity Engineering (HVE) workshop.

The repository includes the first walking slice of the tool. Participants use
that working slice to practice evolving a requirement and an existing codebase
during the workshop.

What gets built is a small .NET console tool that uploads a file to Azure Blob
Storage. It runs entirely against the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
local emulator, so no Azure subscription, no credentials, and no network egress
are required.

> [!NOTE]
> Everything here is fictional. The "Northwind Rail Pension Scheme" does not
> exist, the sample data is synthetic, and no personal data of any kind is
> present in this repository.

## Why this repository exists

The workshop covers an AI-assisted delivery lifecycle: intake, refinement,
research, planning, implementation, review, and release. Teaching that needs a
real piece of work, not a finished sample to read.

So the repository remains deliberately incomplete. There is a requirement, a
backlog, and an implemented walking skeleton. Participants take later stories
through refinement, discover what they do not yet say, and evolve the first
slice.

The interesting part is not the upload. It is that the story cannot be
implemented as written until the team decides what should happen when a file of
that name is already there. Refuse, replace, or version. Each answer produces a
different command, a different runbook, and a different definition of success.
Nobody has decided, and the code cannot avoid the question. The team must also
decide whether the first consumer is an interactive person or an unattended
scheduler, and who owns destination configuration after the local Azurite
slice. Those decisions remain intentionally open for refinement.

## The backlog

| Issue | Type | Role in the day |
|-------|------|-----------------|
| [#1](../../issues/1) | Feature | The parent story. Deliberately **not** Ready: three design decisions are left open so refinement has real work to do. |
| [#2](../../issues/2) | Task | The walking skeleton, sized to fit the implementation window. |

Issue #1 carries the context, acceptance criteria, security lenses, and the
target implementation shape. Read it first.

## Getting started

Prerequisites: the .NET 8 SDK, and either Node.js or Docker for the emulator.

Build the solution and run the default tests without Azurite:

```bash
dotnet build
dotnet test --filter "Category!=Integration"
```

Start the emulator:

```bash
npx azurite@3.36.0 --silent --location .azurite
```

Or with Docker:

```bash
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite:3.36.0
```

With Azurite running, run the opt-in integration tests:

```bash
dotnet test --filter "Category=Integration"
```

Upload the synthetic acceptance fixture:

```bash
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

```text
Uploaded contributions-2026-07.csv to contributions (165 bytes)
```

## Repository layout

```text
src/SchemeFileUploader/          library: upload policy, storage seam, result model
src/SchemeFileUploader.Cli/      console entry point and argument parsing
tests/SchemeFileUploader.Tests/  policy tests plus opt-in emulator tests
data/                            synthetic sample input
```

Tests are split deliberately. Policy behaviour is verified through an in-memory
fake behind the `IBlobUploader` seam and runs in CI with no emulator. Integration
tests exercise the real SDK against Azurite and are excluded from the default
test run by the `Integration` trait.

## A note on the connection string

The tool is configured with `UseDevelopmentStorage=true`, the published Azurite
development shorthand. It is not a secret and it authenticates against nothing
real. No part of this repository connects to an Azure account.

## Contributing

This is workshop material rather than a maintained product. The gaps in the
backlog are intentional and should not be closed on `main` outside a workshop
run.

## Trademarks

This project may contain trademarks or logos for projects, products, or
services. Authorized use of Microsoft trademarks or logos is subject to and must
follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must
not cause confusion or imply Microsoft sponsorship. Any use of third-party
trademarks or logos is subject to those third parties' policies.
