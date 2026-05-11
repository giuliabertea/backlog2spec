using System.Text;
using System.Text.Json;
using Backlog2Spec.Cli.Models;
using Spectre.Console;


namespace Backlog2Spec.Cli.Output;

public sealed class OutputRenderer : IOutputRenderer
{
    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void RenderProgress(string step)
    {
        AnsiConsole.MarkupLine($"[grey]→[/] [dim]{Markup.Escape(step)}[/]");
    }

    public void RenderSpec(GeneratedSpec spec, bool verbose)
    {
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold blue]── Summary ──────────────────────────────────────[/]");
        AnsiConsole.MarkupLine($"[white]{Markup.Escape(spec.Summary)}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold blue]── Acceptance Criteria ──────────────────────────[/]");
        foreach (var ac in spec.AcceptanceCriteria)
            AnsiConsole.MarkupLine($"[white]  • {Markup.Escape(ac)}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold blue]── Edge Cases ───────────────────────────────────[/]");
        foreach (var ec in spec.EdgeCases)
            AnsiConsole.MarkupLine($"[yellow]  ⚠ {Markup.Escape(ec)}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold blue]── Out of Scope ─────────────────────────────────[/]");
        AnsiConsole.MarkupLine($"[white]{Markup.Escape(spec.OutOfScope)}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold blue]── Component Breakdown ──────────────────────────[/]");
        foreach (var comp in spec.ComponentBreakdown)
            AnsiConsole.MarkupLine($"[white]  • {Markup.Escape(comp)}[/]");
        AnsiConsole.WriteLine();
    }

    public void RenderVerboseDetail(EnrichedTicket enriched)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold blue]── Enrichment Detail ────────────────────────────[/]");

        if (enriched.Ambiguities.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Ambiguities:[/]");
            foreach (var a in enriched.Ambiguities)
                AnsiConsole.MarkupLine($"[red]  ? {Markup.Escape(a)}[/]");
        }

        if (enriched.MissingAcceptanceCriteria.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Missing Acceptance Criteria:[/]");
            foreach (var m in enriched.MissingAcceptanceCriteria)
                AnsiConsole.MarkupLine($"[white]  • {Markup.Escape(m)}[/]");
        }

        if (enriched.Constraints.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Constraints:[/]");
            foreach (var c in enriched.Constraints)
                AnsiConsole.MarkupLine($"[white]  • {Markup.Escape(c)}[/]");
        }

        AnsiConsole.WriteLine();
    }

    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]Error:[/] [red]{Markup.Escape(message)}[/]");
    }

    public void RenderRaw(GeneratedSpec spec)
    {
        var json = JsonSerializer.Serialize(spec, PrettyJson);
        Console.WriteLine(json);
    }

    public void RenderMarkdown(GeneratedSpec spec, string title, int workItemId, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Spec: {title}");
        sb.AppendLine();
        sb.AppendLine($"> Work Item: #{workItemId}  ");
        sb.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine(spec.Summary);
        sb.AppendLine();
        sb.AppendLine("## Acceptance Criteria");
        sb.AppendLine();
        foreach (var ac in spec.AcceptanceCriteria)
        {
            sb.AppendLine("```gherkin");
            sb.AppendLine(ac);
            sb.AppendLine("```");
            sb.AppendLine();
        }
        sb.AppendLine("## Edge Cases");
        sb.AppendLine();
        foreach (var ec in spec.EdgeCases)
            sb.AppendLine($"- {ec}");
        sb.AppendLine();
        sb.AppendLine("## Out of Scope");
        sb.AppendLine();
        sb.AppendLine(spec.OutOfScope);
        sb.AppendLine();
        sb.AppendLine("## Component Breakdown");
        sb.AppendLine();
        foreach (var comp in spec.ComponentBreakdown)
        {
            var colonIdx = comp.IndexOf(':');
            var line = colonIdx > 0
                ? $"- **{comp[..colonIdx]}**:{comp[colonIdx..]}"
                : $"- {comp}";
            sb.AppendLine(line);
        }

        var path = outputPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? outputPath
            : outputPath + ".md";

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AnsiConsole.MarkupLine($"[grey]→[/] [dim]Spec saved to {Markup.Escape(path)}[/]");
    }
}
