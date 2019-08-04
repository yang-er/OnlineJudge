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

        public static EntityTypeBuilder<T> UseAttributes<T>(this EntityTypeBuilder<T> entity, bool isMySql) where T : class
        {
            var type = typeof(T);
            var keys = new List<string>();

            foreach (var prop in type.GetProperties())
            {
                var propBuilder = entity.Property(prop.Name);

                if (prop.TryGetAttribute<IgnoreAttribute>(out _))
                    entity.Ignore(prop.Name);

                if (prop.TryGetAttribute<KeyAttribute>(out var ka))
                {
                    keys.Add(prop.Name);
                    if (ka.ValueGeneratedNever)
                        propBuilder.ValueGeneratedNever();
                }

                if (prop.TryGetAttribute<IndexAttribute>(out _))
                    entity.HasIndex(prop.Name);

                if (prop.TryGetAttribute<HasOneWithManyAttribute>(out var fkey))
                    entity.HasOne(fkey.RelatedTypeName)
                        .WithMany()
                        .HasForeignKey(prop.Name)
                        .OnDelete(fkey.DeleteBehavior);

                if (prop.TryGetAttribute<IsRequiredAttribute>(out _))
                    propBuilder.IsRequired();

                if (prop.TryGetAttribute<NonUnicodeAttribute>(out var nup))
                {
                    propBuilder.IsUnicode(false);
                    if (nup.MaxLength != -1)
                        propBuilder.HasMaxLength(nup.MaxLength);
                    if (nup.MaxLength > 65535 && isMySql)
                        propBuilder.HasColumnType("MEDIUMTEXT");
                    else if (nup.MaxLength > 4000 && isMySql)
                        propBuilder.HasColumnType("TEXT");
                }

                if (prop.TryGetAttribute<MaxLengthAttribute>(out var mla))
                {
                    propBuilder.HasMaxLength(mla.MaxLength);

                    if (mla.MaxLength > 65535 && isMySql)
                    {
                        if (prop.PropertyType == typeof(string))
                            propBuilder.HasColumnType("MEDIUMTEXT");
                        else if (prop.PropertyType == typeof(byte[]))
                            propBuilder.HasColumnType("MEDIUMBLOB");
                    }
                    else if (mla.MaxLength > 4000 && isMySql)
                    {
                        if (prop.PropertyType == typeof(string))
                            propBuilder.HasColumnType("TEXT");
                        else if (prop.PropertyType == typeof(byte[]))
                            propBuilder.HasColumnType("BLOB");
                    }
                }

                if (isMySql && (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?)))
                    propBuilder.HasColumnType("bit");
            }

            if (keys.Count > 0)
            {
                entity.HasKey(keys.ToArray());
            }

            return entity;
        }
    }
}
