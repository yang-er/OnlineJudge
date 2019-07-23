using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace JudgeWeb.Data
{
    internal static class EntityBuildingHelper
    {
        private static bool TryGetAttribute<T>(this PropertyInfo propertyInfo, out T value) where T : Attribute
        {
            value = propertyInfo.GetCustomAttribute<T>();
            return value != null;
        }

        public static EntityTypeBuilder<T> UseAttributes<T>(this EntityTypeBuilder<T> entity) where T : class
        {
            var type = typeof(T);
            var keys = new List<string>();

            foreach (var prop in type.GetProperties())
            {
                if (prop.TryGetAttribute<IgnoreAttribute>(out _))
                    entity.Ignore(prop.Name);

                if (prop.TryGetAttribute<KeyAttribute>(out _))
                    keys.Add(prop.Name);

                if (prop.TryGetAttribute<IndexAttribute>(out _))
                    entity.HasIndex(prop.Name);

                if (prop.TryGetAttribute<HasOneWithManyAttribute>(out var fkey))
                {
                    entity.HasOne(fkey.RelatedTypeName)
                        .WithMany()
                        .HasForeignKey(prop.Name)
                        .OnDelete(fkey.DeleteBehavior);
                }

                if (prop.TryGetAttribute<PropertyAttribute>(out var prop2))
                {
                    var prop3 = entity.Property(prop.Name);
                    if (prop2.IsRequired)
                        prop3.IsRequired();
                    if (prop2.IsUnicode == false)
                        prop3.IsUnicode(false);
                    if (prop2.MaxLength != -1)
                        prop3.HasMaxLength(prop2.MaxLength);
                    if (prop2.ValueGeneratedNever)
                        prop3.ValueGeneratedNever();
                }
            }

            if (keys.Count > 0)
            {
                entity.HasKey(keys.ToArray());
            }

            return entity;
        }

        public static ModelBuilder Entity2<T>(this ModelBuilder modelBuilder) where T : class
        {
            return modelBuilder.Entity<T>(entity => entity.UseAttributes());
        }
    }
}
