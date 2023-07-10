namespace Flowqe.Backend.API.GraphQL.FinancesStatistics;

#pragma warning disable CS8618
public record FinanceStatistic
{
    public GroupingKey GroupingKey { get; set; }
    public ICollection<Expense> Expenses { get; set; }
    public decimal TotalAmount { get; set; }
    public double? TotalQuantity { get; set; }
}

public record GroupingKey
{
    public DateTime DateFrom { get; set; }
    public string GroupingId { get; set; }
    public int? ExpenseTypeId { get; set; }
}

public record ExpenseWithDataForGrouping
{
    public GroupingKey GroupingKey { get; set; }
    public Expense Expense { get; set; }
}

// Aggregate by: day, month, year
// Group by: company, project, client
public enum DateScale
{
    Day,
    Month,
    Year,
}

public enum EntityGroup
{
    Company,
    Project,
    Client,
}

public record GroupingModel
{
    public DateScale DateScale { get; set; }
    public EntityGroup EntityGroup { get; set; }
    public bool GroupByExpenseType { get; set; }
}
#pragma warning restore CS8618
