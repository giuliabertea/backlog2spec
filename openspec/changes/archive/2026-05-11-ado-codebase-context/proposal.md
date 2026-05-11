## Why

Wiki pages document intent but lag behind the actual codebase — class names, interfaces, and patterns found in source files are the ground truth a developer needs when implementing a ticket. Grounding the enrichment prompt in real repository files produces component breakdowns and constraints that match the live codebase instead of outdated or missing documentation.

## What Changes

- **BREAKING** Remove `WikiContextAgent`, `IWikiContextAgent`, `MockWikiContextAgent`, and `WikiPageDto`
- **BREAKING** Replace `ado.wikiName` config field with `ado.repoName` (string) and `ado.branch` (string, default `master`)
- **BREAKING** Change `IEnrichmentAgent.EnrichAsync` third parameter from `IReadOnlyList<WikiPageDto>` to `IReadOnlyList<CodeFileDto>`
- Add `CodebaseContextAgent` that queries the ADO Git REST API: lists the repo tree, scores file paths against ticket keywords, fetches content of top matching files (capped at 800 chars each, max 3 files)
- Add `MockCodebaseContextAgent` returning a static code snippet for demo and test use
- Update enrichment prompt to use `## Codebase Context` section instead of `## Relevant Wiki Context`
- Feature remains opt-in: if `ado.repoName` is absent the context step is silently skipped

## Capabilities

### New Capabilities
- `ado-codebase-context`: Queries the ADO Git Items REST API to retrieve source files relevant to a work item; uses keyword scoring on file paths; gracefully degrades to empty list on any error
- `mock-codebase-context`: In-memory stub returning a fixed `CodeFileDto` for offline demo and test pipelines

### Modified Capabilities
- `enrichment-agent`: `EnrichAsync` interface changes — third parameter type changes from `IReadOnlyList<WikiPageDto>` to `IReadOnlyList<CodeFileDto>`; prompt updated to present codebase snippets instead of wiki pages
- `config-loader`: `AdoConfig` gains two new optional fields — `RepoName` (string?) and `Branch` (string?, defaults to `"master"` when repoName is set)

## Impact

- `src/Backlog2Spec.Cli/Ado/WikiPageDto.cs` — deleted
- `src/Backlog2Spec.Cli/Agents/IWikiContextAgent.cs` — deleted
- `src/Backlog2Spec.Cli/Agents/WikiContextAgent.cs` — deleted
- `src/Backlog2Spec.Cli/Agents/MockWikiContextAgent.cs` — deleted
- `src/Backlog2Spec.Cli/Ado/CodeFileDto.cs` — new
- `src/Backlog2Spec.Cli/Agents/ICodebaseContextAgent.cs` — new
- `src/Backlog2Spec.Cli/Agents/CodebaseContextAgent.cs` — new
- `src/Backlog2Spec.Cli/Agents/MockCodebaseContextAgent.cs` — new
- `src/Backlog2Spec.Cli/Config/AgentConfig.cs` — `AdoConfig` updated
- `src/Backlog2Spec.Cli/Agents/IEnrichmentAgent.cs` — signature change
- `src/Backlog2Spec.Cli/Agents/EnrichmentAgent.cs` — uses `CodeFileDto`, updated `BuildPrompt`
- `src/Backlog2Spec.Cli/Agents/MockEnrichmentAgent.cs` — signature update
- `src/Backlog2Spec.Cli/Commands/SpecCommand.cs` — injects `ICodebaseContextAgent` instead of `IWikiContextAgent`
- `src/Backlog2Spec.Cli/Program.cs` — registers `CodebaseContextAgent`
- `src/Backlog2Spec.Cli/Prompts/enrichment.txt` — `## Relevant Wiki Context` → `## Codebase Context`
- `backlog-2-spec.json` — `wikiName` → `repoName` + `branch`
- No new NuGet dependencies — ADO Git Items API is plain HTTP, same auth as existing `WikiContextAgent`
