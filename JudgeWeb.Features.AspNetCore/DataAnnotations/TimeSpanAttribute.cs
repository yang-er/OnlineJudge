namespace System.ComponentModel.DataAnnotations
{
    public class TimeSpanAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return true;
            if (!(value is string realValue)) return false;
            if (string.IsNullOrEmpty(realValue)) return true;
            return realValue.TryParseAsTimeSpan(out _);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Error parsing the format of timespan {name}.";
        }
    }
}

namespace System
{
    public static class TimeSpanAttributeHelper
    {
        public static bool TryParseAsTimeSpan(this string s, out TimeSpan? value)
        {
            value = default;
            if (string.IsNullOrEmpty(s)) return true;
            if (!s.StartsWith('+') && !s.StartsWith('-')) return false;
            var ts = s.Substring(1).Split(':', 3, StringSplitOptions.None);
            if (ts.Length != 3) return false;
            if (!int.TryParse(ts[0], out int hour)) return false;
            if (hour < 0) return false;
            if (!int.TryParse(ts[1], out int minutes)) return false;
            if (minutes < 0 || minutes >= 60) return false;
            if (!int.TryParse(ts[2], out int secs)) return false;
            if (secs < 0 || secs >= 60) return false;
            value = new TimeSpan(hour, minutes, secs);
            if (s.StartsWith('-')) value = -value;
            return true;
        }

        public static string ToDeltaString(this TimeSpan timeSpan)
        {
            char abs = timeSpan.TotalMilliseconds < 0 ? '-' : '+';
            timeSpan = timeSpan.Duration();
            return $"{abs}{Math.Floor(timeSpan.TotalHours)}:{timeSpan.Minutes:d2}:{timeSpan.Seconds:d2}";
        }
    }
}
