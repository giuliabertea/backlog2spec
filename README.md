# Backlog2Spec

A CLI tool that turns an Azure DevOps work item into a ready-to-use, structured spec — in seconds.

Given a work item ID, it fetches the ticket from ADO, enriches it with AI (filling in missing acceptance criteria, edge cases, and ambiguities), optionally pulls relevant source files from your repository for grounding, then generates a Gherkin-style spec tailored to your project's stack and conventions.

The output renders in the terminal with syntax highlighting, can be saved as a markdown file, or piped as JSON for automation.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- An Azure DevOps account with access to your project
- An AI endpoint — either an [Azure AI Foundry](https://ai.azure.com) project with a deployed model, or a classic Azure OpenAI resource

---

## Setup

Follow these steps in order. Steps 1–2 are one-time Azure setup. Steps 3–5 are one-time per machine. Steps 6–7 are one-time per project and should be committed to your project repo.

### Step 1 — Create an Azure AI Foundry project

> **Team note:** You can create one shared project for the whole team (simpler — one endpoint and API key to distribute) or each developer can create their own (fully isolated quota). Either approach works; the secrets setup in Step 5 is identical either way.

1. Go to [ai.azure.com](https://ai.azure.com) and sign in with your Azure account.
2. Click **New project**, give it a name, and let it create a hub and resource group.
3. Inside the project, go to **Model catalog** → search for `gpt-4o` → **Deploy**.
4. Give the deployment a name (e.g. `gpt-4o`) and confirm.
5. Once deployed, go to **Project → Settings → Keys and Endpoints** and note:
   - **Endpoint URL** — looks like `https://<name>.<region>.inference.ai.azure.com`
   - **API Key**
   - **Deployment name** — whatever you used in step 4

You will need these three values in Step 5.

> If you are using a **classic Azure OpenAI resource** instead, use the endpoint from your Azure OpenAI resource (format: `https://your-resource.openai.azure.com`) and omit `AzureAI:EndpointType` (or set it to `AzureOpenAI`).

---

### Step 2 — Create an Azure DevOps PAT

Each developer needs their own Personal Access Token.

1. Go to `https://dev.azure.com/{your-org}/_usersSettings/tokens` → **New Token**.
2. Configure it:

   | Setting | Value |
   |---|---|
   | Name | `backlog2spec` |
   | Expiration | 1 year (set a calendar reminder to renew) |
   | Work Items | **Read** |
   | Code | **Read** — only required if you set `repoName` in the config file |

3. Copy the token — you will not see it again.

---

### Step 3 — Clone this repo and install the tool

```bash
git clone https://github.com/giuliabertea/backlog2spec
cd Backlog2Spec

dotnet pack src/Backlog2Spec.Cli -o ./nupkg
dotnet tool install --local --add-source ./nupkg Backlog2Spec.Cli
```

The `--local` flag installs the tool into your current project's tool manifest (`.config/dotnet-tools.json`). If your project does not have one yet, create it first:

```bash
# Run this inside YOUR project directory, not the Backlog2Spec repo
dotnet new tool-manifest
```

Then go back to the Backlog2Spec repo and run the `dotnet pack` / `dotnet tool install` commands above.

Alternatively, install globally to avoid the manifest requirement:

```bash
dotnet tool install --global --add-source ./nupkg Backlog2Spec.Cli
```

Verify the installation:

```bash
dotnet backlog-2-spec --version
```

---

### Step 4 — Add the config file to your project

Create `backlog-2-spec.json` in the root of **your project** (not the Backlog2Spec repo). The tool searches upward from the current directory to find it.

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

Required fields: `ado.organization`, `ado.project`, `project.name`. The `repoName` and `branch` fields are optional — when set, the tool fetches relevant source files from your repo and feeds them as context to the AI.

**Commit this file to your project repo.** It contains no secrets (only org name and project name). Committing it means every developer who clones your project gets the config automatically.

```bash
git add backlog-2-spec.json
git commit -m "add backlog-2-spec config"
```

---

### Step 5 — Set secrets

Credentials are stored via `dotnet user-secrets` — never in files. **Each developer must run this on their own machine.** Secrets are stored by the OS in a per-user location and are never shared or committed.

Run these commands from inside the **Backlog2Spec repo**:

```bash
cd path/to/Backlog2Spec/src/Backlog2Spec.Cli

# Azure AI Foundry (recommended)
dotnet user-secrets set "AzureAI:Endpoint"       "https://<name>.<region>.inference.ai.azure.com"
dotnet user-secrets set "AzureAI:ApiKey"         "your-api-key"
dotnet user-secrets set "AzureAI:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureAI:EndpointType"   "AzureFoundry"

# Classic Azure OpenAI resource (omit AzureAI:EndpointType or set it to "AzureOpenAI")
dotnet user-secrets set "AzureAI:Endpoint"       "https://your-resource.openai.azure.com"
dotnet user-secrets set "AzureAI:ApiKey"         "your-api-key"
dotnet user-secrets set "AzureAI:DeploymentName" "gpt-4o"

# Azure DevOps PAT (required regardless of endpoint type)
dotnet user-secrets set "Ado:Pat"                "your-ado-pat"
```

> **Migrating from a previous version?** The secret keys were renamed:
> `AzureOpenAI:Endpoint` → `AzureAI:Endpoint`, `AzureOpenAI:ApiKey` → `AzureAI:ApiKey`, `AzureOpenAI:DeploymentName` → `AzureAI:DeploymentName`.
> Re-run the `dotnet user-secrets set` commands above with the new names.

---

### Step 6 — Verify the setup

Before making any real ADO or AI calls, run a mock smoke test from inside **your project directory**:

```bash
cd path/to/your-project
dotnet backlog-2-spec spec 1 --mock
```

This runs the full pipeline with no external calls. If it prints a spec, your config file is found and parsed correctly. Proceed to real usage only after this passes.

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

### Dry run without external calls

```bash
dotnet backlog-2-spec spec 12345 --mock
```

Runs the full pipeline with mock implementations — no ADO or AI calls. Useful for testing config and output formatting.

---

## Keeping the tool up to date

When you pull changes from this repo, rebuild and reinstall:

```bash
cd path/to/Backlog2Spec
git pull
dotnet pack src/Backlog2Spec.Cli -o ./nupkg
dotnet tool update --local --add-source ./nupkg Backlog2Spec.Cli
```

Use `--global` instead of `--local` if you installed globally.

---

## What you gain

**Specs in minutes, not hours.** Writing a complete Gherkin spec from scratch for a mid-size ticket easily takes 30–60 minutes. Backlog2Spec does it in under a minute, with a result that already matches your project's naming conventions, test framework, and architecture.

**Catches what tickets miss.** Most backlog items skip edge cases, have underspecified acceptance criteria, or leave ambiguities implicit. The enrichment step surfaces these explicitly — so the spec you get is already more thorough than what the ticket contained.

**Grounded in your actual codebase.** When `repoName` is configured, the tool fetches files relevant to the ticket from your ADO repository and includes them as context. The generated spec references real components, existing patterns, and the correct layer boundaries — not generic placeholders.

**Consistent across the team.** Every spec produced by the tool follows the same structure and style. Gherkin scenarios, component lists, test strategy — all formatted the same way, regardless of who runs it.

---

## Mock mode

```bash
dotnet backlog-2-spec spec 12345 --mock
```

Mock mode replaces every external dependency — ADO client, enrichment agent, spec generator — with fast stub implementations that return fixed data. No credentials are required and no network calls are made.

Use it to:
- Verify your `backlog-2-spec.json` config is found and parsed correctly
- Test output formatting and rendering without waiting for real AI responses
- Try the full pipeline in CI or on a machine without secrets configured

Mock mode is detected at startup (before the DI container is built), so it works even if `AzureAI:*` secrets are not set.

---

## Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `Configuration error: 'backlog-2-spec.json' not found` | No config file in CWD or any parent | Create `backlog-2-spec.json` in your project root |
| `Missing required field: ado.organization` | Config file incomplete | Add the missing field |
| `Authentication error: Failed to connect to Azure DevOps` | Invalid PAT or org URL | Re-set `Ado:Pat` and verify `ado.organization` |
| `Authentication error: Authentication failed` | PAT expired or wrong scope | Generate a new PAT with Work Items: Read (and Code: Read if using repo context) |
| `AI response error: LLM returned invalid JSON` | Model returned malformed JSON after 3 retries | Check deployment name and quota; try again |
| `Unexpected error: AzureAI:Endpoint secret is missing` | User secrets not set | Run the secrets setup commands in Step 5 |
| `No manifest file found` | Missing `.config/dotnet-tools.json` in project | Run `dotnet new tool-manifest` in your project root, then reinstall |
