using System.Text.Json.Serialization;

namespace Backlog2Spec.Cli.Models;

public sealed class GeneratedSpec
{
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("acceptanceCriteria")]
    public List<string> AcceptanceCriteria { get; init; } = [];

    [JsonPropertyName("edgeCases")]
    public List<string> EdgeCases { get; init; } = [];

    [JsonPropertyName("outOfScope")]
    public string OutOfScope { get; init; } = string.Empty;

    [JsonPropertyName("componentBreakdown")]
    public List<string> ComponentBreakdown { get; init; } = [];
}
