using Microsoft.SemanticKernel;

namespace Backlog2Spec.Cli.Kernel;

public sealed class KernelFactory
{
    public Microsoft.SemanticKernel.Kernel Build(string endpoint, string apiKey, string deploymentName)
    {
        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: endpoint,
            apiKey: apiKey);

        return builder.Build();
    }
}
