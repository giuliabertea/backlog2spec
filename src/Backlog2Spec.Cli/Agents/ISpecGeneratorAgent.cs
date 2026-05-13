using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Models;

namespace Backlog2Spec.Cli.Agents;

public interface ISpecGeneratorAgent
{
    Task<GeneratedSpec> GenerateAsync(
        EnrichedTicket enriched,
        AgentConfig config,
        IReadOnlyList<CodeFileDto> codebaseContext,
        CancellationToken ct = default);
}
