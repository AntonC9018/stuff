using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Conventions;

public delegate string RenamingConvention(string defaultName);

public static class ConventionHelper
{
    public static void SetColumnNamesByConventionIfNotSet(
        this ModelBuilder modelBuilder,
        RenamingConvention convention)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            {
                var tableName = entity.GetTableName() ?? throw new NotImplementedException();
                var newName = convention(tableName);
                entity.SetTableName(newName);
            }

            foreach (var p in entity.GetProperties())
            {
                var name = p.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value;
                if (name is null)
                {
                    string defaultName;
                    {
                        var table = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);
                        if (table is { } tableValue)
                        {
#pragma warning disable EF1001 // internal API usage
                            var overrides = RelationalPropertyOverrides.Find(p, tableValue);
                            if (overrides?.IsColumnNameOverridden == true)
                                defaultName = overrides.ColumnName;
                            else
                                defaultName = p.GetDefaultColumnName(tableValue);
#pragma warning restore EF1001
                        }
                        else
                            defaultName = p.GetDefaultColumnName();
                    }

                    var conventionalName = convention(defaultName);
                    p.SetColumnName(conventionalName);
                }
            }

            foreach (var k in entity.GetKeys())
            {
                // Cannot check if it's been set without copy-pasting too much, so just check for the default.
                var defaultName = k.GetDefaultName();
                var name = k.GetName();
                if (name == defaultName)
                {
                    var conventionalName = convention(defaultName);
                    k.SetName(conventionalName);
                }
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.FindAnnotation(RelationalAnnotationNames.Name)?.Value;
                if (name is null)
                {
                    var defaultName = fk.GetDefaultName() ?? throw new NotImplementedException();
                    var conventionalName = convention(defaultName);
                    fk.SetConstraintName(conventionalName);
                }
            }

            foreach (var i in entity.GetIndexes())
            {
                var name = i.FindAnnotation(RelationalAnnotationNames.Name)?.Value ?? i.Name;
                if (name is null)
                {
                    var defaultName = i.GetDefaultDatabaseName() ?? throw new NotImplementedException();
                    var conventionalName = convention(defaultName);
                    i.SetDatabaseName(conventionalName);
                }
            }
        }
    }
}
