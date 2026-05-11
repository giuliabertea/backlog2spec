using System.CommandLine;
using System.CommandLine.Invocation;
using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Agents;
using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Exceptions;
using Backlog2Spec.Cli.Output;
using Backlog2Spec.Cli.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Backlog2Spec.Cli.Commands;

public sealed class SpecCommand : Command
{
    private readonly IAdoClient _adoClient;
    private readonly ICodebaseContextAgent _codebaseContextAgent;
    private readonly IEnrichmentAgent _enrichmentAgent;
    private readonly ISpecGeneratorAgent _specGeneratorAgent;
    private readonly IOutputRenderer _renderer;
    private readonly ConfigLoader _configLoader;
    private readonly ILogger<SpecCommand> _logger;
    private readonly TokenUsageTracker _tokenTracker;

    public SpecCommand(
        IAdoClient adoClient,
        ICodebaseContextAgent codebaseContextAgent,
        IEnrichmentAgent enrichmentAgent,
        ISpecGeneratorAgent specGeneratorAgent,
        IOutputRenderer renderer,
        ConfigLoader configLoader,
        ILogger<SpecCommand> logger,
        TokenUsageTracker tokenTracker)
        : base("spec", "Generate a structured spec from an Azure DevOps work item")
    {
        _adoClient = adoClient;
        _codebaseContextAgent = codebaseContextAgent;
        _enrichmentAgent = enrichmentAgent;
        _specGeneratorAgent = specGeneratorAgent;
        _renderer = renderer;
        _configLoader = configLoader;
        _logger = logger;
        _tokenTracker = tokenTracker;

        var idArg = new Argument<int>("id", "Azure DevOps work item ID");
        var verboseOption = new Option<bool>("--verbose", "Show additional enrichment detail");
        var rawOption = new Option<bool>("--raw", "Output JSON only, no formatting");
        var mockOption = new Option<bool>("--mock", "Run pipeline with mock implementations (no external calls)");
        var outputOption = new Option<string?>("--output", "Save spec to a markdown file at the given path");
        var budgetOption = new Option<decimal?>("--budget", "Monthly spend limit in USD (default: $20.00)");
        budgetOption.AddValidator(result =>
        {
            var val = result.GetValueOrDefault<decimal?>();
            if (val.HasValue && val.Value <= 0)
                result.ErrorMessage = "--budget must be a positive value greater than zero";
        });

        AddArgument(idArg);
        AddOption(verboseOption);
        AddOption(rawOption);
        AddOption(mockOption);
        AddOption(outputOption);
        AddOption(budgetOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var id = context.ParseResult.GetValueForArgument(idArg);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var raw = context.ParseResult.GetValueForOption(rawOption);
            var mock = context.ParseResult.GetValueForOption(mockOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var budget = context.ParseResult.GetValueForOption(budgetOption);
            var exitCode = await ExecuteAsync(id, verbose, raw, mock, output, budget, CancellationToken.None);
            Environment.Exit(exitCode);
        });
    }

    private async Task<int> ExecuteAsync(
        int id, bool verbose, bool raw, bool mock, string? output, decimal? budget, CancellationToken ct)
    {
        if (budget.HasValue)
            _tokenTracker.BudgetLimit = budget.Value;

        try
        {
            _tokenTracker.EnforceBudget();

            if (!raw) _renderer.RenderProgress("Loading configuration...");
            var config = await _configLoader.LoadAsync(ct);

            if (!raw) _renderer.RenderProgress($"Fetching work item #{id}...");
            _logger.LogInformation("Fetching work item {WorkItemId}", id);
            var workItem = await _adoClient.GetWorkItemAsync(id, ct);

            if (!raw && !string.IsNullOrEmpty(config.Ado.RepoName))
                _renderer.RenderProgress("Fetching codebase context...");
            _logger.LogInformation("Fetching codebase context for work item {WorkItemId}", id);
            var codebaseContext = await _codebaseContextAgent.FetchRelevantFilesAsync(workItem, config, ct);

            if (!raw) _renderer.RenderProgress("Enriching ticket...");
            _logger.LogInformation("Enriching work item {WorkItemId}", id);
            var enriched = await _enrichmentAgent.EnrichAsync(workItem, config, codebaseContext, ct);

            if (!raw && verbose) _renderer.RenderVerboseDetail(enriched);

            if (!raw) _renderer.RenderProgress("Generating spec...");
            _logger.LogInformation("Generating spec for work item {WorkItemId}", id);
            var spec = await _specGeneratorAgent.GenerateAsync(enriched, config, ct);

            if (raw)
            {
                _renderer.RenderRaw(spec);
            }
            else
            {
                _renderer.RenderSpec(spec, verbose);
                if (!string.IsNullOrEmpty(output))
                    _renderer.RenderMarkdown(spec, enriched.Title, enriched.WorkItemId, output);
            }

            return 0;
        }
        catch (BudgetExceededException ex)
        {
            _logger.LogError(
                "Budget exceeded. Current cost: {CurrentCost:F2}, Limit: {BudgetLimit:F2}",
                ex.CurrentCost, ex.BudgetLimit);
            AnsiConsole.Write(new Panel(
                $"Current cost: ${ex.CurrentCost:F2}\nLimit: ${ex.BudgetLimit:F2}\n\nTip: Reduce prompt size or increase --budget.")
            {
                Header = new PanelHeader("Budget Exceeded"),
                BorderStyle = new Style(Color.Red)
            });
            return 1;
        }
        catch (ConfigException ex)
        {
            _logger.LogError(ex, "Configuration error");
            _renderer.RenderError($"Configuration error: {ex.Message}");
            return 1;
        }
        catch (AdoNotFoundException ex)
        {
            _logger.LogError(ex, "Work item not found");
            _renderer.RenderError(ex.Message);
            return 1;
        }
        catch (AdoAuthException ex)
        {
            _logger.LogError(ex, "ADO authentication error");
            _renderer.RenderError($"Authentication error: {ex.Message}");
            return 1;
        }
        catch (LlmFormatException ex)
        {
            _logger.LogError(ex, "LLM returned invalid JSON");
            _renderer.RenderError($"AI response error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            _renderer.RenderError($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}
