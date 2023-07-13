using System.Reflection;
using AutoImplementedProperties.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Type = System.Type;

#pragma warning disable CS8618

public sealed class Company
{
    public int Id { get; set; }
    public ICollection<Task> Tasks { get; set; }
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
    public readonly List<Type> EntityTypes = new();
    public readonly List<(Type Interface, Delegate? Delegate)> InterfaceConfigurations = new();

    public TableSplitEntityBuilder(ModelBuilder modelBuilder)
    {
        _modelBuilder = modelBuilder;
    }

    public TableSplitEntityBuilder Entity<T>(Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        configure?.Invoke(_modelBuilder.Entity<T>());
        EntityTypes.Add(typeof(T));
        return this;
    }
    
    public TableSplitEntityBuilder Interface<T>(Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        if (typeof(T).IsInterface == false)
            throw new InvalidOperationException("Type must be an interface.");
        
        InterfaceConfigurations.Add((typeof(T), configure));
        return this;
    }
}

public static class Helper
{
    public static ModelBuilder SplitTable<TMainEntity>(
        this ModelBuilder modelBuilder,
        Action<EntityTypeBuilder<TMainEntity>, TableSplitEntityBuilder> configure)

        where TMainEntity : class
    {
        var tableBuilder = new TableSplitEntityBuilder();
        var mainEntityModelBuilder = modelBuilder.Entity<TMainEntity>();
        configure(mainEntityModelBuilder, tableBuilder);

        Dictionary<string, List<Type>> propertiesToContainingEntities = new();
        Dictionary<Type, List<PropertyInfo>> interfaceToPropertyMap = new();
        HashSet<Type> allInterfaces = new();
        List<List<Type>> entityToInterfaces = new();
        foreach (var entityType in tableBuilder.EntityTypes)
        {
            var interfaces = entityType.GetInterfaces();
            var interfacesList = new List<Type>();

            foreach (var i in interfaces)
            {
                if (interfaceToPropertyMap.ContainsKey(i))
                    continue;

                var properties = i
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p is { CanWrite: true, CanRead: true })
                    .ToList();

                if (properties.Count == 0)
                    continue;

                interfaceToPropertyMap.Add(i, properties);

                foreach (var property in properties)
                {
                    if (!propertiesToContainingEntities.TryGetValue(property.Name, out var entities))
                    {
                        entities = new List<Type>();
                        propertiesToContainingEntities.Add(property.Name, entities);
                    }

                    entities.Add(entityType);
                }
            }

            allInterfaces.UnionWith(interfaces);
            entityToInterfaces.Add(interfacesList);
        }

        var mainEntityInterfaces = typeof(TMainEntity)
            .GetInterfaces()
            .ToHashSet();

        var missingInterfacesInMainEntity = allInterfaces
            .Where(x => !mainEntityInterfaces.Contains(x))
            .ToList();

        if (missingInterfacesInMainEntity.Any())
        {
            throw new Exception("Missing interfaces in main entity: "
                                + string.Join(", ", missingInterfacesInMainEntity.Select(x => x.Name)));
        }

        var mainEntityModel = mainEntityModelBuilder.Metadata;
        var mainEntityPrimaryKey = mainEntityModel.FindPrimaryKey();
        if (mainEntityPrimaryKey is null)
            throw new Exception("Main entity primary key is null");
        var mainEntityPrimaryKeyPropertyNames = mainEntityPrimaryKey.Properties.Select(p => p.Name).ToArray();

        var entityTypes = tableBuilder.EntityTypes;
        for (int i = 0; i < entityTypes.Count; i++)
        {
            var mainEntityNavigation = entityTypes[i]
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(x => x.PropertyType == typeof(TMainEntity));
            if (mainEntityNavigation is null)
                throw new Exception("Main entity navigation is required");

            var mainEntityNavigationName = mainEntityNavigation.Name;

            var entityBuilder = modelBuilder.Entity(entityTypes[i]);
            var entityModel = entityBuilder.Metadata;

            for (int j = 0; j < i; j++)
            {
                entityBuilder
                    .HasOne(entityTypes[j])
                    .WithOne()
                    .HasForeignKey(entityTypes[j], mainEntityPrimaryKeyPropertyNames);
            }

            entityBuilder
                .HasOne(typeof(TMainEntity), mainEntityNavigationName)
                .WithOne()
                .HasForeignKey(entityTypes[i], mainEntityPrimaryKeyPropertyNames);
            entityBuilder
                .Navigation(mainEntityNavigationName)
                .IsRequired();

            foreach (var @interface in entityToInterfaces[i])
            {
                var properties = interfaceToPropertyMap[@interface];
                foreach (var property in properties)
                {
                    var mainEntityProperty = mainEntityModel.GetProperty(property.Name);
                    var propertyModel = entityModel.GetProperty(property.Name);
                    propertyModel.IsNullable = mainEntityProperty.IsNullable;
                    propertyModel.ValueGenerated = mainEntityProperty.ValueGenerated;
                    propertyModel.IsConcurrencyToken = mainEntityProperty.IsConcurrencyToken;
                    propertyModel.SetColumnName(mainEntityProperty.GetColumnName());
                    propertyModel.SetColumnType(mainEntityProperty.GetColumnType());
                    propertyModel.SetMaxLength(mainEntityProperty.GetMaxLength());
                    propertyModel.SetPrecision(mainEntityProperty.GetPrecision());
                    propertyModel.SetScale(mainEntityProperty.GetScale());
                    propertyModel.SetIsUnicode(mainEntityProperty.IsUnicode());
                    propertyModel.SetDefaultValueSql(mainEntityProperty.GetDefaultValueSql());
                    propertyModel.SetComputedColumnSql(mainEntityProperty.GetComputedColumnSql());
                    propertyModel.SetDefaultValue(mainEntityProperty.GetDefaultValue());
                    propertyModel.SetValueConverter(mainEntityProperty.GetValueConverter());
                    propertyModel.SetValueComparer(mainEntityProperty.GetValueComparer());
                    propertyModel.SetValueGeneratorFactory(mainEntityProperty.GetValueGeneratorFactory());
                    propertyModel.SetBeforeSaveBehavior(mainEntityProperty.GetBeforeSaveBehavior());
                    propertyModel.SetAfterSaveBehavior(mainEntityProperty.GetAfterSaveBehavior());
                    propertyModel.SetProviderClrType(mainEntityProperty.GetProviderClrType());
                    foreach (var annotation in mainEntityProperty.GetAnnotations())
                        propertyModel.AddAnnotation(annotation.Name, annotation.Value);
                }
            }
        }

        var entitiesToProcess = new HashSet<Type>();

        void CacheEntities(IReadOnlyList<IMutableProperty> properties)
        {
            using var e = properties.GetEnumerator();
            if (!e.MoveNext())
                return;
            foreach (var entity in propertiesToContainingEntities[e.Current.Name])
                entitiesToProcess.Add(entity);
            while (e.MoveNext())
                entitiesToProcess.IntersectWith(propertiesToContainingEntities[e.Current.Name]);
        }

        foreach (var index in mainEntityModel.GetIndexes())
        {
            var properties = index.Properties.Select(p => p.Name).ToArray();
            CacheEntities(index.Properties);

            foreach (var entity in entitiesToProcess)
            {
                var entityBuilder = modelBuilder.Entity(entity);
                var indexBuilder = entityBuilder.HasIndex(properties);
                indexBuilder.HasDatabaseName(index.GetDatabaseName());
                indexBuilder.HasFilter(index.GetFilter());
                indexBuilder.IsUnique(index.IsUnique);
                // indexBuilder.HasAnnotation("SqlServer:Include", index.GetIncludeProperties());
            }

            entitiesToProcess.Clear();
        }

        // Do the same thing with foreign keys
        foreach (var foreignKey in mainEntityModel.GetForeignKeys())
        {
            var properties = foreignKey.Properties.Select(p => p.Name).ToArray();
            CacheEntities(foreignKey.Properties);

            foreach (var entity in entitiesToProcess)
            {
                var entityBuilder = modelBuilder.Entity(entity);
                var entityModel = entityBuilder.Metadata;

                var newForeignKey = entityModel.AddForeignKey(
                    properties.Select(p => entityModel.GetProperty(p)).ToArray(),
                    foreignKey.PrincipalKey,
                    foreignKey.PrincipalEntityType);
                // Copy configuration
                newForeignKey.DeleteBehavior = foreignKey.DeleteBehavior;
                newForeignKey.IsRequired = foreignKey.IsRequired;
                newForeignKey.IsUnique = foreignKey.IsUnique;
                newForeignKey.IsOwnership = foreignKey.IsOwnership;
                newForeignKey.IsRequiredDependent = foreignKey.IsRequiredDependent;
            }

            entitiesToProcess.Clear();
        }

        // Do the same thing with navigations
        foreach (var navigation in mainEntityModel.GetNavigations())
        {
            foreach (var entity in propertiesToContainingEntities[navigation.Name])
            {
                var entityBuilder = modelBuilder.Entity(entity);
                entityBuilder.Navigation(navigation.Name);
            }
        }

        return modelBuilder;
    }
}


public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SplitTable<TaskAll>((entity, table) =>
        {
            table.AddEntity<TaskRequired>();
            table.AddEntity<TaskGeneral>();
            
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasOne(x => x.Company).WithMany().OnDelete(DeleteBehavior.Cascade);
        });
    }
}

#pragma warning restore CS8618

