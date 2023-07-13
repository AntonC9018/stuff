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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(new TaskAll());
}

app.MapGet("endpoint", async (IDbContextFactory<ApplicationDbContext> factory) =>
{
    TaskAll taskAll;
    {
        using var dbContext = factory.CreateDbContext();
        var company = new Company { };
        dbContext.Set<Company>().Add(company);
        await dbContext.SaveChangesAsync();
        
        taskAll = new TaskAll
        {
            Company = company,
            Name = "Test",
            SomeOtherProperty = "ABC",
            Description = "NoDescription",
        };
        dbContext.Set<TaskAll>().Add(taskAll);
        await dbContext.SaveChangesAsync();
    }
    {
        using var dbContext = factory.CreateDbContext();
        
        var taskGeneral = dbContext.Set<TaskGeneral>().First(t => t.Id == taskAll.Id);
        taskGeneral.Description = "Description";
        await dbContext.SaveChangesAsync();

        var newTaskAll = dbContext.Set<TaskAll>().AsNoTracking().First(t => t.Id == taskAll.Id);
        return Results.Json(new
        {
            taskAll, 
            newTaskAll,
            taskGeneral,
        });
    }
});

app.Run();
