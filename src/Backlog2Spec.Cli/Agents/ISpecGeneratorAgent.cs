using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Models;

namespace Backlog2Spec.Cli.Agents;

public interface ISpecGeneratorAgent
{
    Task<GeneratedSpec> GenerateAsync(EnrichedTicket enriched, AgentConfig config, CancellationToken ct = default);
}
