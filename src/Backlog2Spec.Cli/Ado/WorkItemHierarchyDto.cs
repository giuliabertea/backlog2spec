namespace Backlog2Spec.Cli.Ado;

public sealed record WorkItemHierarchyDto(
    WorkItemDto Parent,
    IReadOnlyList<WorkItemDto> Children);
