using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;
using WorkOfficeApi.DataAccessLayer.Entities.Common;

namespace WorkOfficeApi.DataAccessLayer.Extensions;

public static class ModelBuilderExtensions
{
    private static readonly MethodInfo queryFilterMethod;
    private static readonly ValueConverter<string, string> trimStringConverter;

    static ModelBuilderExtensions()
    {
        queryFilterMethod = typeof(DataContext).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(t => t.IsGenericMethod && t.Name == "ApplyQueryFilter");

        trimStringConverter = new ValueConverter<string, string>(v => v.Trim(), v => v.Trim());
    }

    public static ModelBuilder ApplyTrimStringConverter(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    builder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(trimStringConverter);
                }
            }
        }

        return builder;
    }

    public static ModelBuilder ApplyQueryFilter(this ModelBuilder builder, DataContext dataContext)
    {
        var entities = builder.Model
            .GetEntityTypes()
            .Where(t => typeof(DeletableEntity).IsAssignableFrom(t.ClrType))
            .ToList();

        foreach (var type in entities.Select(t => t.ClrType))
        {
            var methods = SetGlobalQueryMethods(type);

            foreach (var method in methods)
            {
                var genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(dataContext, new object[] { builder });
            }
        }

        return builder;
    }

    private static IEnumerable<MethodInfo> SetGlobalQueryMethods(Type type)
    {
        var result = new List<MethodInfo>();

        if (typeof(DeletableEntity).IsAssignableFrom(type))
        {
            result.Add(queryFilterMethod);
        }

        return result;
    }
}