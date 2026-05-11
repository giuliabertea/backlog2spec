namespace Backlog2Spec.Cli.Ado;

public interface IAdoClient
{
    Task<WorkItemDto> GetWorkItemAsync(int id, CancellationToken ct = default);
}
