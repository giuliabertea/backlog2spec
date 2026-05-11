using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;
using Backlog2Spec.Cli.Models;

namespace Backlog2Spec.Cli.Agents;

public interface IEnrichmentAgent
{
    Task<EnrichedTicket> EnrichAsync(
        WorkItemDto workItem,
        AgentConfig config,
        IReadOnlyList<CodeFileDto> codebaseContext,
        CancellationToken ct = default);
}
