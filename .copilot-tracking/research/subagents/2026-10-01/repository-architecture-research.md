<!-- markdownlint-disable-file -->

# Repository Architecture Research

## Research scope

* Complete local and upstream repository structure, refs, default branch, tags, and recent currency
* Language, framework, dependencies, runtime architecture, and data flows
* Azure hosting, deployment, and infrastructure as code
* Existing security, authentication, storage, and upload functionality
* Tests, conventions, README intent, and workshop constraints
* Exact owning files, symbols or headings, and line ranges
* Sparse local checkout differences from upstream
* Implementation-oriented options for security, authentication, and random file or folder upload

## Status

In progress. Local and upstream tree/ref baselines verified; backlog evidence and
implementation implications remain under investigation.

## Evidence

### E1 Local Git and worktree

* `git status --short --branch` reports `main...origin/main`; the only untracked
	path is `.copilot-tracking/`, created for this research.
* `git sparse-checkout list` fails with `fatal: this worktree is not sparse`.
* `git ls-tree -r --name-only HEAD` returns exactly `.gitignore` and `README.md`.
* `origin` fetch and push URLs are
	`https://github.com/Azure-Samples/hve-workshop-demo.git`.
* Local `HEAD`, `main`, `origin/main`, and `origin/HEAD` resolve to
	`f39214dfe05704445a85e43a44cf12208ccced89`.

### E2 Upstream metadata and tree

* GitHub repository metadata reports public, non-archived, default branch
	`main`, language `null`, two open issues, and custom repository properties
	`activeRepoStatus=false` and `currentRepoStatus=false`.
* Upstream contains one unprotected branch, `main`, at
	`f39214dfe05704445a85e43a44cf12208ccced89`, and no tags.
* The recursive upstream tree is complete (`truncated=false`) and contains only
	`.gitignore` (blob `d5a18deed8813c6c817c9090bf0443d7fad48a9d`, 7,454 bytes)
	and `README.md` (blob `99f579f3f7c19fbe4e74e093e549cf588ab2f59f`,
	4,382 bytes).
* Sources:
	[repository API](https://api.github.com/repos/Azure-Samples/hve-workshop-demo),
	[branches API](https://api.github.com/repos/Azure-Samples/hve-workshop-demo/branches?per_page=100),
	[tags API](https://api.github.com/repos/Azure-Samples/hve-workshop-demo/tags?per_page=100), and
	[recursive tree API](https://api.github.com/repos/Azure-Samples/hve-workshop-demo/git/trees/main?recursive=1).

### E3 Commit currency

* The latest commit is
	`f39214dfe05704445a85e43a44cf12208ccced89`, authored and committed on
	2026-08-16T17:32:25Z with subject `docs: reframe README for greenfield build
	rather than defect fix`; it is unsigned according to the GitHub API.
* Earlier commits are `c86f0642cd6b0547dbb1cb1453745da6753d8de5`
	(2026-08-16T17:24:20Z, README addition) and
	`699bd3fab9f4ca8f97adae5ba7ec78b8912ef963` (2026-08-16T17:21:09Z,
	initial commit).
* GitHub reports `pushed_at` 2026-08-16T17:32:25Z and repository metadata
	`updated_at` 2026-09-25T09:57:09Z. The latter does not imply a source push.
* Sources: [commits API](https://api.github.com/repos/Azure-Samples/hve-workshop-demo/commits?per_page=100)
	and [latest commit](https://github.com/Azure-Samples/hve-workshop-demo/commit/f39214dfe05704445a85e43a44cf12208ccced89).

## Findings

### Repository completeness

The local checkout is not sparse in Git's technical sense. It is a complete
checkout of an intentionally skeletal upstream commit. No local/upstream source
tree difference is present at the verified commit.

## Unresolved gaps

Pending.

## Recommended next research

Pending.
