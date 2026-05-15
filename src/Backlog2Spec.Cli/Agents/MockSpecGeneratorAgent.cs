using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Models;

namespace Backlog2Spec.Cli.Agents;

public sealed class MockSpecGeneratorAgent : ISpecGeneratorAgent
{
    public Task<GeneratedSpec> GenerateAsync(
        EnrichedTicket enriched,
        AgentConfig config,
        IReadOnlyList<CodeFileDto> codebaseContext,
        CancellationToken ct = default)
    {
        return Task.FromResult(new GeneratedSpec
        {
            Goal = "Add a mock feature to validate the pipeline end-to-end. The mock returns fixed data to allow testing without an LLM call.",
            Behaviour =
            [
                "Return a fixed spec when called with any enriched ticket",
                "Return an error result when input is flagged as invalid"
            ],
            EdgeCases = ["Null input", "Extremely large payload"],
            OutOfScope = "Authentication, Authorization",
            FilesToChange =
            [
                "src/Backlog2Spec.Cli/Agents/MockSpecGeneratorAgent.cs: return mock GeneratedSpec",
                "src/Backlog2Spec.Cli/Agents/ISpecGeneratorAgent.cs: interface contract"
            ]
        });
    }
}
