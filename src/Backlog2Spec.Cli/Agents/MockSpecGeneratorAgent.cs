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
            Summary = "Mock spec for testing pipeline",
            AcceptanceCriteria =
            [
                "Given valid input, When processed, Then a success response is returned",
                "Given invalid input, When processed, Then an error is returned"
            ],
            EdgeCases = ["Null input", "Extremely large payload"],
            OutOfScope = "Authentication, Authorization",
            ComponentBreakdown = ["API Endpoint", "Application Service", "Validation Layer"]
        });
    }
}
