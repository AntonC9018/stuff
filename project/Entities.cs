using System.Reflection;
using AutoImplementedProperties.Attributes;
using Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvalidOperationException = System.InvalidOperationException;
using Type = System.Type;

#pragma warning disable CS8618

public sealed class Company
{
    public int Id { get; set; }
}

public interface ITaskBase
{
    int Id { get; set; }
    int CompanyId { get; set; }
    Company Company { get; set; }
}

public interface ITaskRequired : ITaskBase
{
    string Name { get; set; }
}

public interface ITaskGeneral : ITaskBase
{
    DateTime? StartDate { get; set; }
    string? Description { get; set; }
}

[AutoImplementProperties]
public sealed partial class TaskRequired : ITaskRequired
{
    public TaskAll Task { get; set; }
}

[AutoImplementProperties]
public sealed partial class TaskGeneral : ITaskGeneral
{
    public TaskAll Task { get; set; }
}

[AutoImplementProperties]
public sealed partial class TaskAll : ITaskRequired, ITaskGeneral
{
    public string SomeOtherProperty { get; set; }
}

public sealed class TableSplitEntityBuilder
{
    private readonly ModelBuilder _modelBuilder;
    private readonly List<EntityTypeBuilder> _entityBuilders = new();
    private readonly List<(Type Interface, Delegate? Delegate)> _sharedPartialConfigurations = new();
    private EntityTypeBuilder? _mainEntityType;
    
    internal IReadOnlyList<EntityTypeBuilder> EntityBuilders 
        => _entityBuilders;
    internal IReadOnlyList<(Type Interface, Delegate? Delegate)> SharedPartialConfigurations 
        => _sharedPartialConfigurations;
    internal EntityTypeBuilder? MainEntityType 
        => _mainEntityType;

    public TableSplitEntityBuilder(ModelBuilder modelBuilder)
    {
        _modelBuilder = modelBuilder;
    }
    
    public TableSplitEntityBuilder MainEntity<T>(Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        if (_mainEntityType is not null)
            throw new InvalidOperationException("Main entity already configured.");
        
        var builder = _modelBuilder.Entity<T>();
        _mainEntityType = builder;
        configure?.Invoke(builder);
        
        return this;
    }

    public TableSplitEntityBuilder Entity<T>(Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        var builder = _modelBuilder.Entity<T>();
        configure?.Invoke(builder);

        if (EntityBuilders.All(x => x.Metadata.ClrType != typeof(T))
            && _mainEntityType?.Metadata.ClrType != typeof(T))
        {
            _entityBuilders.Add(builder);
        }
        
        return this;
    }
    
    public TableSplitEntityBuilder Partial<T>(Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        if (typeof(T).IsInterface == false)
            throw new InvalidOperationException("Type must be an interface.");
        
        _sharedPartialConfigurations.Add((typeof(T), configure));
        return this;
    }
}

public static class Helper
{
    public static ModelBuilder SplitTable(
        this ModelBuilder modelBuilder,
        string tableName,
        Action<TableSplitEntityBuilder> configure)
    {
        var tableBuilder = new TableSplitEntityBuilder(modelBuilder);
        configure(tableBuilder);
        
        var mainEntityBuilder = tableBuilder.MainEntityType;
        if (mainEntityBuilder is null)
            throw new InvalidOperationException("Configure main entity with MainEntity<T> method.");
        var mainEntityModel = mainEntityBuilder.Metadata;

        mainEntityBuilder.ToTable(tableName);
        
        var entityBuilders = tableBuilder.EntityBuilders;
        var interfaceConfigurations = tableBuilder.SharedPartialConfigurations;
        
        var entityTypesByInterface = interfaceConfigurations
            .SelectMany(i => entityBuilders
                .Append(mainEntityBuilder)
                .Where(e => i.Interface.IsAssignableFrom(e.Metadata.ClrType))
                .Select(e => (i.Interface, EntityType: e)))
            .ToLookup(x => x.Interface, x => x.EntityType);

        if (tableBuilder.SharedPartialConfigurations.Count > 0)
        {
            var args = new object?[2];

            foreach (var i in tableBuilder.SharedPartialConfigurations)
            {
                using var entityTypesForInterface = entityTypesByInterface[i.Interface].GetEnumerator();
                if (!entityTypesForInterface.MoveNext())
                    throw new InvalidOperationException($"No entity types found for interface {i.Interface}.");
                
                do
                {
                    var entityType = entityTypesForInterface.Current;
                    args[0] = entityType.Metadata;
                    args[1] = i.Delegate;
                    _ConfigureEntityMethod.MakeGenericMethod(i.Interface)
                        .Invoke(null, args);
                }
                while (entityTypesForInterface.MoveNext());
            }
        }

        var idProperties = mainEntityModel
            .GetKeys()
            .Where(k => k.IsPrimaryKey())
            .Select(k => k.Properties)
            .First();
        var idPropertyNames = idProperties
            .Select(x => x.Name)
            .ToArray();
        
        // Make sure these properties are in all entities
        foreach (var entityBuilder in entityBuilders)
        {
            var entityModel = entityBuilder.Metadata;
            var entityProperties = entityModel.GetProperties();
            var missingProperties = idPropertyNames
                .Where(x => entityProperties.All(p => p.Name != x))
                .ToArray();
            foreach (var missingProperty in missingProperties)
            {
                var property = mainEntityModel.FindProperty(missingProperty)!;
                if (entityModel.FindProperty(property.Name) is null)
                    entityModel.AddProperty(property.Name, property.ClrType);
            }
        }

        for (int i = 0; i < entityBuilders.Count; i++)
        {
            var entityBuilder = entityBuilders[i];
            var mainEntityNavigation = entityBuilders[i]
                .Metadata
                .GetDeclaredNavigations()
                .FirstOrDefault(x => x.ClrType == mainEntityModel.ClrType);
            if (mainEntityNavigation is null)
                throw new Exception("Main entity navigation is required");

            var entityType = entityBuilder.Metadata.ClrType;
            var mainEntityNavigationName = mainEntityNavigation.Name;

            for (int j = 0; j < i; j++)
            {
                var otherType = entityBuilders[j].Metadata.ClrType;
                entityBuilder
                    .HasOne(otherType)
                    .WithOne()
                    .HasForeignKey(otherType, idPropertyNames);
            }

            entityBuilder
                .HasOne(mainEntityModel.ClrType, mainEntityNavigationName)
                .WithOne()
                .HasForeignKey(mainEntityModel.ClrType, idPropertyNames);
            entityBuilder
                .HasKey(idPropertyNames);
            entityBuilder
                .Navigation(mainEntityNavigationName)
                .IsRequired();
            entityBuilder
                .ToTable(tableName);
        }

        return modelBuilder;
    }
    
    private static readonly MethodInfo _ConfigureEntityMethod = typeof(Helper)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
        .Single(x => x.Name == nameof(ConfigureEntity));

    private static void ConfigureEntity<TInterface>(
        IMutableEntityType entityType,
        Action<EntityTypeBuilder<TInterface>> interfaceConfiguration)
    
        where TInterface : class
    {
#pragma warning disable EF1001 // internal API usage
        var builder = new EntityTypeBuilder<TInterface>(entityType);
#pragma warning restore EF1001
        interfaceConfiguration(builder);
    }
}


public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SplitTable(tableName: "Test", table =>
        {
            table.MainEntity<TaskAll>();
            // table.Entity<TaskRequired>();
            table.Entity<TaskGeneral>();

            table.Partial<ITaskRequired>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100);
            });
            table.Partial<ITaskGeneral>(entity =>
            {
                entity.Property(x => x.Description).HasMaxLength(1000);
            });
            table.Partial<ITaskBase>(entity =>
            {
                entity.HasOne(x => x.Company).WithMany().OnDelete(DeleteBehavior.Cascade);
            });
        });
        modelBuilder.SetColumnNamesByConventionIfNotSet(s => s);
    }
}

#pragma warning restore CS8618

