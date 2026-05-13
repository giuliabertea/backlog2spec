using Backlog2Spec.Cli.Ado;

namespace Backlog2Spec.Cli.Agents;

public interface IKeywordExtractor
{
    Task<IReadOnlyList<string>> ExtractAsync(WorkItemDto workItem, CancellationToken ct = default);
}
