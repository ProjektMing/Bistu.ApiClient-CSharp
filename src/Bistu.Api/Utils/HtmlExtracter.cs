using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Bistu.Api.Utils;

internal static partial class HtmlExtracter
{
    private const int ExecutionTokenLength = 2205;
    private static readonly Range range = ^2537..^(2537 - ExecutionTokenLength);

    public static (string, string) Extract(string html)
    {
        //ExecutionValueRegex().Match(html).Groups["execution"].Value;
        return (html[range], "");
    }

    [GeneratedRegex(@"(?<=execution[^>]+)value=""(?<execution>.+)""", RegexOptions.RightToLeft)]
    private static partial Regex ExecutionValueRegex();
}
