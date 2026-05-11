namespace Backlog2Spec.Cli.Exceptions;

public sealed class BudgetExceededException : Exception
{
    public decimal CurrentCost { get; }
    public decimal BudgetLimit { get; }

    public BudgetExceededException(decimal currentCost, decimal budgetLimit)
        : base($"Budget exceeded. Current cost: ${currentCost:F2}, Limit: ${budgetLimit:F2}.")
    {
        CurrentCost = currentCost;
        BudgetLimit = budgetLimit;
    }
}
