using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class IndexAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class IgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class PropertyAttribute : Attribute
    {
        public bool IsRequired { get; set; }

        public int MaxLength { get; set; } = -1;

        public bool IsUnicode { get; set; } = true;

        public bool ValueGeneratedNever { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class KeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class HasOneWithManyAttribute : Attribute
    {
        public string RelatedTypeName { get; set; }

        public DeleteBehavior DeleteBehavior { get; set; }

        public HasOneWithManyAttribute(Type type, DeleteBehavior behavior)
        {
            RelatedTypeName = type.FullName;
            DeleteBehavior = behavior;
        }
    }
}
