using Microsoft.AspNetCore.Html;
using System;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlContent Timestamp(this IHtmlHelper hh, long timestamp)
        {
            return hh.CstTime(DateTimeOffset.UnixEpoch.AddSeconds(timestamp));
        }

        public static string Timespan(this IHtmlHelper hh, TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 1) return $"{timeSpan.TotalDays:0} days ago";
            else if (timeSpan.TotalHours > 1) return $"{timeSpan.TotalHours:0} hours ago";
            else if (timeSpan.TotalMinutes > 1) return $"{timeSpan.TotalMinutes:0} mins ago";
            return $"{timeSpan.TotalSeconds:0} secs ago";
        }

        public static IHtmlContent CstTime(this IHtmlHelper hh, DateTimeOffset dt)
        {
            return hh.Raw(dt.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public static IHtmlContent RatioOf(this IHtmlHelper hh, int fz, int fm)
        {
            return hh.Raw(fm == 0 ? "0.00% (0/0)" : $"{100.0 * fz / fm:F2}% ({fz}/{fm})");
        }

        public static IHtmlContent NiceByteSize(this IHtmlHelper hh, long size)
        {
            if (size > 1048576) return hh.Raw($"{size / 1048576.0:F2}M");
            else if (size > 1024) return hh.Raw($"{size / 1024.0:F2}K");
            return hh.Raw($"{size}B");
        }
    }
}
