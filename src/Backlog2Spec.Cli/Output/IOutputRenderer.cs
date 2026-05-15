using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Models;

namespace Backlog2Spec.Cli.Output;

public interface IOutputRenderer
{
    void RenderProgress(string step);
    void RenderSpec(GeneratedSpec spec, bool verbose);
    void RenderVerboseDetail(EnrichedTicket enriched);
    void RenderError(string message);
    void RenderRaw(GeneratedSpec spec);
    void RenderMarkdown(GeneratedSpec spec, string title, int workItemId, string outputPath);
    void WriteHierarchyToFiles(WorkItemDto parent, IEnumerable<(WorkItemDto Item, GeneratedSpec Spec)> children, string outputDir);
}
