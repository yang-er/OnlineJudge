﻿namespace System.ComponentModel.DataAnnotations
{
    public class DateTimeAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return true;
            if (!(value is string realValue)) return false;
            if (string.IsNullOrWhiteSpace(realValue)) return true;
            return DateTimeOffset.TryParse(realValue, out _);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Error parsing the format of datetime {name}.";
        }
    }
}
