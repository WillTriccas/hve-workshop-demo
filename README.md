# Scheme File Uploader

Lab repository for a half-day Hyper Velocity Engineering (HVE) workshop.

The tool does not exist yet. That is the point. Participants build it during the
workshop, starting from a requirement and an empty solution rather than from an
existing codebase.

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

So the repository ships deliberately incomplete. There is a requirement, a
backlog, and a skeleton. There is no implementation. Participants take the story
through refinement, discover what it does not yet say, and build the first slice.

The interesting part is not the upload. It is that the story cannot be
implemented as written until the team decides what should happen when a file of
that name is already there. Refuse, replace, or version. Each answer produces a
different command, a different runbook, and a different definition of success.
Nobody has decided, and the code cannot avoid the question.

## The backlog

| Issue | Type | Role in the day |
|-------|------|-----------------|
| [#1](../../issues/1) | Feature | The parent story. Deliberately **not** Ready: three design decisions are left open so refinement has real work to do. |
| [#2](../../issues/2) | Task | The walking skeleton, sized to fit the implementation window. |

Issue #1 carries the context, acceptance criteria, security lenses, and the
target implementation shape. Read it first.

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

There is nothing to run yet. Building the thing that runs is the exercise.

## Target shape

Once the first slice lands:

```bash
dotnet build
dotnet test --filter "Category!=Integration"
dotnet run --project src/SchemeFileUploader.Cli -- data/contributions-2026-07.csv
```

```text
Uploaded contributions-2026-07.csv (2,048 bytes)
```

## Planned repository layout

```text
src/SchemeFileUploader/          library: upload policy, storage seam, result model
src/SchemeFileUploader.Cli/      console entry point and argument parsing
tests/SchemeFileUploader.Tests/  policy tests plus one opt-in emulator test
data/                            synthetic sample input
docs/requirements/               the product requirements the backlog came from
docs/lab-brief.md                participant handout
```

Tests are split deliberately. Policy behaviour is verified through an in-memory
fake behind the `IBlobUploader` seam and runs in CI with no emulator. A single
integration test exercises the real SDK against Azurite and is excluded from the
default test run by the `Integration` trait.

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
