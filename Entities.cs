using Flowqe.Backend.API.GraphQL.FinancesStatistics;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618

public sealed class Expense
{
    public int Id { get; set; }
    public int ExpenseTypeId { get; set; }
    public int CompanyId { get; set; }

    public DateTime Date { get; set; }
        
    public ExpenseType ExpenseType { get; set; }
    public ICollection<ExpenseCost> ExpenseCosts { get; set; }
    public Company Company { get; set; }
}

public sealed class ExpenseType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Expense> Expenses { get; set; }
}

public sealed class ExpenseCost
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public decimal Price { get; set; }
    public double Quantity { get; set; }
    public Expense Expense { get; set; }
}

public sealed class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Expense> Expenses { get; set; }
}

public sealed class ExpenseStatistic
{
    public GroupingKey GroupingKey { get; set; }
    public ICollection<Expense> Expenses { get; set; }
    public decimal TotalAmount { get; set; }
    public double? TotalQuantity { get; set; }
}

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<ExpenseCost> ExpenseCosts { get; set; }
    public DbSet<Company> Companies { get; set; }
    
    // Seed data
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasData(new Company[]
        {
            new()
            {
                Id = 1,
                Name = "Fizz",
            },
            new()
            {
                Id = 2,
                Name = "Buzz",
            },
        });
        modelBuilder.Entity<ExpenseType>().HasData(new ExpenseType[]
        {
            new()
            {
                Id = 1,
                Name = "Transport",
            },
            new()
            {
                Id = 2,
                Name = "Food",
            },
        });
        modelBuilder.Entity<Expense>().HasData(new Expense[]
        {
            new()
            {
                Id = 1,
                CompanyId = 1,
                ExpenseTypeId = 1,
                Date = new DateTime(2021, 1, 1),
            },
            new()
            {
                Id = 2,
                CompanyId = 1,
                ExpenseTypeId = 1,
                Date = new DateTime(2021, 1, 1),
            },
            new()
            {
                Id = 3,
                CompanyId = 1,
                ExpenseTypeId = 2,
                Date = new DateTime(2022, 10, 2),
            },
            new()
            {
                Id = 4,
                CompanyId = 1,
                ExpenseTypeId = 2,
                Date = new DateTime(2022, 10, 3),
            },
            new()
            {
                Id = 5,
                CompanyId = 2,
                ExpenseTypeId = 1,
                Date = new DateTime(2022, 11, 4),
            },
            new()
            {
                Id = 6,
                CompanyId = 2,
                ExpenseTypeId = 1,
                Date = new DateTime(2023, 2, 2),
            },
            new()
            {
                Id = 7,
                CompanyId = 2,
                ExpenseTypeId = 2,
                Date = new DateTime(2023, 3, 1),
            },
            new()
            {
                Id = 8,
                CompanyId = 2,
                ExpenseTypeId = 2,
                Date = new DateTime(2023, 3, 2),
            },
        });
        modelBuilder.Entity<ExpenseCost>().HasData(new ExpenseCost[]
        {
            new()
            {
                Id = 1,
                ExpenseId = 1,
                Price = 10,
                Quantity = 1,
            },
            new()
            {
                Id = 2,
                ExpenseId = 2,
                Price = 20,
                Quantity = 2,
            },
            new()
            {
                Id = 3,
                ExpenseId = 3,
                Price = 30,
                Quantity = 3,
            },
            new()
            {
                Id = 4,
                ExpenseId = 4,
                Price = 40,
                Quantity = 4,
            },
            new()
            {
                Id = 5,
                ExpenseId = 5,
                Price = 50,
                Quantity = 5,
            },
            new()
            {
                Id = 6,
                ExpenseId = 6,
                Price = 60,
                Quantity = 6,
            },
            new()
            {
                Id = 7,
                ExpenseId = 7,
                Price = 70,
                Quantity = 7,
            },
            new()
            {
                Id = 8,
                ExpenseId = 8,
                Price = 80,
                Quantity = 8,
            },
            new()
            {
                Id = 9,
                ExpenseId = 1,
                Price = 90,
                Quantity = 9,
            },
            new()
            {
                Id = 10,
                ExpenseId = 2,
                Price = 100,
                Quantity = 10,
            },
            new()
            {
                Id = 11,
                ExpenseId = 3,
                Price = 110,
                Quantity = 11,
            },
            new()
            {
                Id = 12,
                ExpenseId = 4,
                Price = 120,
                Quantity = 12,
            },
            new()
            {
                Id = 13,
                ExpenseId = 5,
                Price = 130,
                Quantity = 13,
            },
        });

        // modelBuilder.Entity<ExpenseStatistic>(entity =>
        // {
        //     
        // });
    }
}

#pragma warning restore CS8618

