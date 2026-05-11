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
}
