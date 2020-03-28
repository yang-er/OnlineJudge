using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace System
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class InjectAttribute : Attribute
    {
        private Type ServiceType { get; }

        private Type ImplementType { get; }

        public InjectAttribute(Type staticType)
        {
            ServiceType = ImplementType = staticType;
        }

        public InjectAttribute(Type abstractType, Type implementType)
        {
            ServiceType = abstractType;
            ImplementType = implementType;
        }

        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

        public ServiceDescriptor GetDescriptior()
        {
            return new ServiceDescriptor(ServiceType, ImplementType, Lifetime);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AssemblyInjectExtensions
    {
        public static IServiceCollection TryAddFrom(this IServiceCollection services, Assembly assembly)
        {
            foreach (var attr in assembly.GetCustomAttributes<System.InjectAttribute>())
                services.TryAdd(attr.GetDescriptior());
            return services;
        }
    }
}
