using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;

namespace Backlog2Spec.Cli.Agents;

public sealed class MockCodebaseContextAgent : ICodebaseContextAgent
{
    public Task<IReadOnlyList<CodeFileDto>> FetchRelevantFilesAsync(
        WorkItemDto workItem, AgentConfig config, CancellationToken ct = default)
    {
        IReadOnlyList<CodeFileDto> files =
        [
            new CodeFileDto
            {
                Path = "/src/Application/Profitability/ProfitabilityService.cs",
                FileName = "ProfitabilityService.cs",
                Content = "public sealed class ProfitabilityService : IProfitabilityService\n{\n    private readonly ISnapshotRepository _snapshotRepo;\n    private readonly IBookingRepository _bookingRepo;\n\n    public ProfitabilityService(\n        ISnapshotRepository snapshotRepo,\n        IBookingRepository bookingRepo)\n    {\n        _snapshotRepo = snapshotRepo;\n        _bookingRepo = bookingRepo;\n    }\n\n    public async Task<ProfitabilityResult> CalculateAsync(int bookingId, CancellationToken ct)\n    {\n        // ...\n    }\n}"
            }
        ];
        return Task.FromResult(files);
    }
}
