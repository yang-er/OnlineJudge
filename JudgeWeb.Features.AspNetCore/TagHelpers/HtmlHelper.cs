﻿using Microsoft.AspNetCore.Html;
using System;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static class HtmlHelperExtensions
    {
        public static string Timespan(this IHtmlHelper hh, TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 730) return $"{timeSpan.TotalDays / 365:0} years ago";
            else if (timeSpan.TotalDays > 60) return $"{timeSpan.TotalDays / 30:0} months ago";
            else if (timeSpan.TotalDays > 14) return $"{timeSpan.TotalDays / 7:0} weeks ago";
            else if (timeSpan.TotalDays > 2) return $"{timeSpan.TotalDays:0} days ago";
            else if (timeSpan.TotalHours > 2) return $"{timeSpan.TotalHours:0} hours ago";
            else if (timeSpan.TotalMinutes > 2) return $"{timeSpan.TotalMinutes:0} mins ago";
            return $"{timeSpan.TotalSeconds:0} secs ago";
        }

        public static IHtmlContent CstTime(this IHtmlHelper hh, DateTimeOffset? dt)
        {
            return dt.HasValue ? hh.Raw(dt.Value.ToString("yyyy/MM/dd HH:mm:ss")) : hh.Raw("");
        }

        public static IHtmlContent Timespan2(this IHtmlHelper hh, TimeSpan? timespan)
        {
            if (!timespan.HasValue) return hh.Raw("-");
            int tot = (int)timespan.Value.TotalSeconds;
            return hh.Raw($"{tot / 60:00}:{tot % 60:00}");
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
