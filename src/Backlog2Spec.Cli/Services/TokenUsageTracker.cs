using Backlog2Spec.Cli.Exceptions;

namespace Backlog2Spec.Cli.Services;

public sealed class TokenUsageTracker
{
    private const decimal InputCostPerMillionTokens = 2.50m;
    private const decimal OutputCostPerMillionTokens = 10.00m;

    private long _inputTokens;
    private long _outputTokens;

    public decimal BudgetLimit { get; set; } = 20m;

    public void AddUsage(int inputTokens, int outputTokens)
    {
        Interlocked.Add(ref _inputTokens, Math.Max(0, inputTokens));
        Interlocked.Add(ref _outputTokens, Math.Max(0, outputTokens));
    }

    public decimal GetEstimatedCost() =>
        (_inputTokens / 1_000_000m * InputCostPerMillionTokens) +
        (_outputTokens / 1_000_000m * OutputCostPerMillionTokens);

    public bool IsBudgetExceeded(decimal limit) => GetEstimatedCost() >= limit;

    public void EnforceBudget()
    {
        if (IsBudgetExceeded(BudgetLimit))
            throw new BudgetExceededException(GetEstimatedCost(), BudgetLimit);
    }
}
