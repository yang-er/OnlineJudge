using Microsoft.EntityFrameworkCore;
using System;

namespace JudgeWeb.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class IndexAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class IgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class IsRequiredAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class ValueGeneratedNeverAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class NonUnicodeAttribute : Attribute
    {
        public int MaxLength { get; set; } = -1;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class MaxLengthAttribute : Attribute
    {
        public int MaxLength { get; } = -1;

        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class KeyAttribute : Attribute
    {
        public bool ValueGeneratedNever { get; set; }
    }

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
