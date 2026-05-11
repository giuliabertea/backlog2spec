namespace Backlog2Spec.Cli.Ado;

public sealed class MockAdoClient : IAdoClient
{
    public Task<WorkItemDto> GetWorkItemAsync(int id, CancellationToken ct = default)
    {
        return Task.FromResult(new WorkItemDto
        {
            Id = id,
            Title = "Mock Work Item",
            Description = "This is a mock description",
            AcceptanceCriteria = "Sample acceptance criteria",
            WorkItemType = "User Story"
        });
    }
}
