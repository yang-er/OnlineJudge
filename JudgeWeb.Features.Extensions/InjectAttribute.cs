using Microsoft.Extensions.DependencyInjection;

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
