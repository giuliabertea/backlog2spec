using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backlog2Spec.Cli.Ado;
using Backlog2Spec.Cli.Config;
using Microsoft.Extensions.Logging;

namespace Backlog2Spec.Cli.Agents;

public sealed class CodebaseContextAgent : ICodebaseContextAgent
{
    private const int MaxFiles = 3;
    private const int ContentMaxChars = 800;

    private static readonly HashSet<string> SourceExtensions =
        [".cs", ".ts", ".js", ".py", ".java", ".go", ".md"];

    private static readonly string[] Stopwords =
    [
        "the", "and", "for", "with", "this", "that", "from", "have",
        "not", "are", "was", "will", "add", "new", "fix", "update",
        "when", "then", "given", "user", "should", "must", "able",
        "into", "onto", "also", "each", "some", "more"
    ];

    private readonly HttpClient _httpClient;
    private readonly ILogger<CodebaseContextAgent> _logger;

    public CodebaseContextAgent(string pat, ILogger<CodebaseContextAgent> logger)
    {
        _logger = logger;
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<IReadOnlyList<CodeFileDto>> FetchRelevantFilesAsync(
        WorkItemDto workItem, AgentConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.Ado.RepoName))
            return [];

        try
        {
            var branch = config.Ado.Branch ?? "master";
            var filePaths = await ListFilePathsAsync(config, branch, ct);
            var keywords = ExtractKeywords(workItem);

            _logger.LogDebug("Codebase search: [{Keywords}] against {Count} files",
                string.Join(", ", keywords), filePaths.Count);

            var topPaths = filePaths
                .Where(p => SourceExtensions.Contains(System.IO.Path.GetExtension(p).ToLowerInvariant()))
                .Select(p => (Path: p, Score: ScorePath(p, keywords)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Path.Length)
                .Take(MaxFiles)
                .Select(x => x.Path)
                .ToList();

            var results = new List<CodeFileDto>();
            foreach (var path in topPaths)
            {
                var file = await FetchFileContentAsync(config, branch, path, ct);
                if (file is not null) results.Add(file);
            }

            _logger.LogInformation("Fetched {Count} source files for codebase context", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Codebase context lookup failed — continuing without codebase context");
            return [];
        }
    }

    private async Task<IReadOnlyList<string>> ListFilePathsAsync(
        AgentConfig config, string branch, CancellationToken ct)
    {
        var url = $"{config.Ado.Organization}/{config.Ado.Project}/_apis/git/repositories/{config.Ado.RepoName}/items" +
                  $"?recursionLevel=full&versionDescriptor.version={Uri.EscapeDataString(branch)}&versionDescriptor.versionType=branch&api-version=7.1";

        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return [];

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var paths = new List<string>();

        if (!doc.RootElement.TryGetProperty("value", out var items)) return [];

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("gitObjectType", out var typeEl)) continue;
            if (typeEl.GetString() != "blob") continue;
            if (!item.TryGetProperty("path", out var pathEl)) continue;
            var path = pathEl.GetString();
            if (!string.IsNullOrEmpty(path))
                paths.Add(path);
        }

        return paths;
    }

    private async Task<CodeFileDto?> FetchFileContentAsync(
        AgentConfig config, string branch, string filePath, CancellationToken ct)
    {
        var encodedPath = Uri.EscapeDataString(filePath);
        var url = $"{config.Ado.Organization}/{config.Ado.Project}/_apis/git/repositories/{config.Ado.RepoName}/items" +
                  $"?path={encodedPath}&includeContent=true&versionDescriptor.version={Uri.EscapeDataString(branch)}&versionDescriptor.versionType=branch&api-version=7.1";

        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.TryGetProperty("content", out var c)
            ? c.GetString() ?? string.Empty
            : string.Empty;

        if (content.Length > ContentMaxChars)
            content = content[..ContentMaxChars] + "...";

        return new CodeFileDto
        {
            Path = filePath,
            FileName = System.IO.Path.GetFileName(filePath),
            Content = content
        };
    }

    private static IReadOnlyList<string> ExtractKeywords(WorkItemDto workItem)
    {
        var text = $"{workItem.Title} {workItem.WorkItemType}";
        return [.. text
            .Split([' ', '-', '_', '/', '\\', '.', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 3 && !Stopwords.Contains(w))
            .Distinct()
            .Take(5)];
    }

    private static int ScorePath(string path, IReadOnlyList<string> keywords)
    {
        var lower = path.ToLowerInvariant();
        return keywords.Count(kw => lower.Contains(kw));
    }
}
