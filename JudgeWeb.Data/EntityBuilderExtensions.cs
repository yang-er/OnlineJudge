using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore
{
    public static class EntityBuilderApplyExtensions
    {
        static readonly Type GenericType = typeof(IEntityTypeConfiguration<>);

        static readonly MethodInfo ApplyConfiguration =
            typeof(ModelBuilder).GetMethods()
            .Where(m => m.Name == nameof(ApplyConfiguration))
            .Where(m => m.GetParameters().FirstOrDefault().ParameterType.IsEntityTypeConfiguration())
            .Single();

        private static bool IsEntityTypeConfiguration(this Type type)
        {
            if (!type.IsConstructedGenericType) return false;
            return type.GetGenericTypeDefinition() == GenericType;
        }

        public static void Apply<TConfiguration>(
            this ModelBuilder modelBuilder)
            where TConfiguration : class, new()
        {
            var configurationInstance = new TConfiguration();
            var interfaces = typeof(TConfiguration).GetInterfaces();
            foreach (var entityConfig in interfaces)
            {
                if (!entityConfig.IsEntityTypeConfiguration())
                    continue;
                var entityType = entityConfig.GetGenericArguments().Single();
                var callingMethod = ApplyConfiguration.MakeGenericMethod(entityType);
                callingMethod.Invoke(modelBuilder, new[] { configurationInstance });
            }
        }
    }
}
