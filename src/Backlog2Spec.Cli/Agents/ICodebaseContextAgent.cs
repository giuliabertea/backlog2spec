using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;

namespace Backlog2Spec.Cli.Agents;

public interface ICodebaseContextAgent
{
    Task<IReadOnlyList<CodeFileDto>> FetchRelevantFilesAsync(
        WorkItemDto workItem, AgentConfig config, CancellationToken ct = default);
}
