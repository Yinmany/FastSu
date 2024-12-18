using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FastSu.Server;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FacadeInjectAttribute : Attribute
{
    public readonly string? Key;

    public FacadeInjectAttribute()
    {
    }

    public FacadeInjectAttribute(string key)
    {
        Key = key;
    }
}

public static class FacadeHelper
{
    public static void Inject(Type type, IServiceProvider sp)
    {
        foreach (var propertyInfo in type.GetProperties())
        {
            FacadeInjectAttribute? attr = propertyInfo.GetCustomAttribute<FacadeInjectAttribute>();
            if (attr is null) continue;

            object obj;
            if (attr.Key is null)
            {
                obj = sp.GetRequiredService(propertyInfo.PropertyType);
            }
            else
            {
                obj = sp.GetRequiredKeyedService(propertyInfo.PropertyType, attr.Key);
            }

            propertyInfo.SetValue(null, obj);
        }
    }
}