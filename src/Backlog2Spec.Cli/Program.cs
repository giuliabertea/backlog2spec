using System.CommandLine;
using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Agents;
using Backlog2Spec.Cli.Commands;
using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Kernel;
using Backlog2Spec.Cli.Output;
using Backlog2Spec.Cli.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

bool isMock = args.Contains("--mock");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, cfg) =>
    {
        cfg.AddUserSecrets<Program>();
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        services.AddSingleton<ConfigLoader>();
        services.AddSingleton<IOutputRenderer, OutputRenderer>();
        services.AddSingleton<TokenUsageTracker>();
        services.AddSingleton<SpecCommand>();

        if (isMock)
        {
            services.AddSingleton<IEnrichmentAgent, MockEnrichmentAgent>();
            services.AddSingleton<ISpecGeneratorAgent, MockSpecGeneratorAgent>();
            services.AddSingleton<IAdoClient, MockAdoClient>();
            services.AddSingleton<ICodebaseContextAgent, MockCodebaseContextAgent>();
        }
        else
        {
            var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint secret is missing.");
            var apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey secret is missing.");
            var deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName secret is missing.");
            var pat = config["Ado:Pat"] ?? throw new InvalidOperationException("Ado:Pat secret is missing.");

            var kernel = new KernelFactory().Build(endpoint, apiKey, deploymentName);

            services.AddSingleton(kernel);
            services.AddSingleton<IEnrichmentAgent, EnrichmentAgent>();
            services.AddSingleton<ISpecGeneratorAgent, SpecGeneratorAgent>();
            services.AddSingleton<IAdoClient>(sp =>
                new AdoClient(sp.GetRequiredService<ConfigLoader>(), pat));
            services.AddSingleton<ICodebaseContextAgent>(sp =>
                new CodebaseContextAgent(pat, sp.GetRequiredService<ILogger<CodebaseContextAgent>>()));
        }

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    })
    .Build();

if (isMock)
{
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MockMode");
    logger.LogInformation("[MOCK MODE ENABLED]");
}

var specCommand = host.Services.GetRequiredService<SpecCommand>();
var rootCommand = new RootCommand("backlog-2-spec — AI-powered spec generator for Azure DevOps work items");
rootCommand.AddCommand(specCommand);

return await rootCommand.InvokeAsync(args);
