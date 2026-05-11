# Backlog2Spec

A CLI tool that turns an Azure DevOps work item into a ready-to-use, structured spec — in seconds.

Given a work item ID, it fetches the ticket from ADO, enriches it with AI (filling in missing acceptance criteria, edge cases, and ambiguities), optionally pulls relevant source files from your repository for grounding, then generates a Gherkin-style spec tailored to your project's stack and conventions.

The output renders in the terminal with syntax highlighting, can be saved as a markdown file, or piped as JSON for automation.

---

## Installation

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8), an Azure DevOps account, and either an [Azure AI Foundry](https://ai.azure.com) project with a deployed model or a classic Azure OpenAI resource (e.g. GPT-4o).

```bash
git clone https://github.com/giuliabertea/backlog2spec
cd Backlog2Spec

dotnet pack src/Backlog2Spec.Cli -o ./nupkg
dotnet tool install --local --add-source ./nupkg Backlog2Spec.Cli
```

Verify:

```bash
dotnet backlog-2-spec --version
```

### Config file

Create `backlog-2-spec.json` in your project root (the tool searches upward from the current directory):

```json
{
  "project": {
    "name": "MyService",
    "language": "C#",
    "framework": ".NET 8 / ASP.NET Core",
    "testFramework": "xUnit",
    "architecture": "Clean Architecture"
  },
  "conventions": {
    "naming": "PascalCase classes, camelCase fields",
    "folderStructure": "Feature-based",
    "specStyle": "Gherkin",
    "diPattern": "Constructor injection"
  },
  "ado": {
    "organization": "https://dev.azure.com/your-org",
    "project": "YourProject",
    "repoName": "YourRepo",
    "branch": "main"
  }
}
```

Required: `ado.organization`, `ado.project`, `project.name`. The `repoName` and `branch` fields are optional — when set, the tool fetches relevant source files from your repo and feeds them as context to the AI.

### Secrets

Credentials are stored via `dotnet user-secrets` — never in files:

```bash
cd src/Backlog2Spec.Cli

# Azure AI Foundry (recommended)
dotnet user-secrets set "AzureAI:Endpoint"       "https://<endpoint>.<region>.inference.ai.azure.com"
dotnet user-secrets set "AzureAI:ApiKey"         "your-api-key"
dotnet user-secrets set "AzureAI:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureAI:EndpointType"   "AzureFoundry"

# Classic Azure OpenAI resource (omit AzureAI:EndpointType or set it to "AzureOpenAI")
dotnet user-secrets set "AzureAI:Endpoint"       "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureAI:ApiKey"         "your-api-key"
dotnet user-secrets set "AzureAI:DeploymentName" "gpt-4o"

dotnet user-secrets set "Ado:Pat"                "your-ado-pat"
```

> **Migrating from a previous version?** The secret keys were renamed:
> `AzureOpenAI:Endpoint` → `AzureAI:Endpoint`, `AzureOpenAI:ApiKey` → `AzureAI:ApiKey`, `AzureOpenAI:DeploymentName` → `AzureAI:DeploymentName`.
> Re-run the `dotnet user-secrets set` commands above with the new names.

Generate a PAT at `https://dev.azure.com/{org}/_usersSettings/tokens` with **Work Items: Read** scope (add **Code: Read** if you use `repoName`).

---

## Usage

### Basic

```bash
dotnet backlog-2-spec spec 12345
```

Fetches work item #12345, enriches it, and prints the spec to the terminal.

### With verbose enrichment detail

```bash
dotnet backlog-2-spec spec 12345 --verbose
```

Shows the AI-identified missing acceptance criteria, edge cases, and ambiguities before the spec.

### Save to markdown

```bash
dotnet backlog-2-spec spec 12345 --output ./specs/feature-12345.md
```

### JSON output (pipe-friendly)

```bash
dotnet backlog-2-spec spec 12345 --raw
dotnet backlog-2-spec spec 12345 --raw | jq .summary
```

### Budget control

```bash
dotnet backlog-2-spec spec 12345 --budget 5.00
```

The tool tracks cumulative token spend and refuses to run once the monthly limit is reached (default: $20.00).

### Dry run without external calls

```bash
dotnet backlog-2-spec spec 12345 --mock
```

Runs the full pipeline with mock implementations — no ADO or AI calls. Useful for testing config and output formatting.

---

## What you gain

**Specs in minutes, not hours.** Writing a complete Gherkin spec from scratch for a mid-size ticket easily takes 30–60 minutes. Backlog2Spec does it in under a minute, with a result that already matches your project's naming conventions, test framework, and architecture.

**Catches what tickets miss.** Most backlog items skip edge cases, have underspecified acceptance criteria, or leave ambiguities implicit. The enrichment step surfaces these explicitly — so the spec you get is already more thorough than what the ticket contained.

**Grounded in your actual codebase.** When `repoName` is configured, the tool fetches files relevant to the ticket from your ADO repository and includes them as context. The generated spec references real components, existing patterns, and the correct layer boundaries — not generic placeholders.

**Consistent across the team.** Every spec produced by the tool follows the same structure and style. Gherkin scenarios, component lists, test strategy — all formatted the same way, regardless of who runs it.

**Cost-aware by default.** The built-in budget tracker accumulates spend across runs and blocks execution if the configured limit is exceeded, so there are no surprise AI bills.

---

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `Configuration error: 'backlog-2-spec.json' not found` | No config file in CWD or any parent | Create `backlog-2-spec.json` in your project root |
| `Missing required field: ado.organization` | Config file incomplete | Add the missing field |
| `Authentication error: Failed to connect to Azure DevOps` | Invalid PAT or org URL | Re-set `Ado:Pat` and verify `ado.organization` |
| `Authentication error: Authentication failed` | PAT expired or wrong scope | Generate a new PAT with Work Items: Read (and Code: Read if using repo context) |
| `AI response error: LLM returned invalid JSON` | Model returned malformed JSON after 3 retries | Check deployment name and quota; try again |
| `Unexpected error: AzureAI:Endpoint secret is missing` | User secrets not set | Run the secrets setup commands above |
