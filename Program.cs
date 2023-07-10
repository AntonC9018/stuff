using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<ApplicationDbContext>(c =>
{
    c.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManagement"));
});

// Internal API usage.
// We have to add both the factory, and the context as a service.
#pragma warning disable EF1001
builder.Services.TryAddSingleton<IDbContextFactory<ApplicationDbContext>>(
    sp => new PooledDbContextFactory<ApplicationDbContext>(
        sp.GetRequiredService<IDbContextPool<ApplicationDbContext>>()));
#pragma warning restore EF1001

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureDeleted();
    dbContext.Database.EnsureCreated();
}

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    o.RoutePrefix = string.Empty;
});

TransactionManager.ImplicitDistributedTransactions = true;

app.MapGet("endpoint", async (IDbContextFactory<ApplicationDbContext> factory) =>
{
    using var context1 = factory.CreateDbContext();
    context1.Companies.Add(new Company { Name = "1" });
    await context1.SaveChangesAsync().ConfigureAwait(false);
    
    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
    {
        using var context2 = factory.CreateDbContext();
        using var context3 = factory.CreateDbContext();
        context3.Companies.Add(new Company { Name = "6" });
        context2.Companies.Add(new Company { Name = "2" });
        context1.Companies.Add(new Company { Name = "3" });
        var tasks = new[]
        {
            context2.SaveChangesAsync(),
            context3.SaveChangesAsync(),
            context1.SaveChangesAsync(),
        };
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
});

app.Run();
