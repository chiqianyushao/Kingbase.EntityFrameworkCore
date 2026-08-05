using System.Text.Json;
using KdbndpTypes;

namespace Microsoft.EntityFrameworkCore;

public static class KingbaseDbFunctionsExtensions
{
    public static string? JsonExtractPathText(this DbFunctions _, JsonDocument json, string path)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static string? JsonExtractPathText(this DbFunctions _, JsonElement json, string path)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool ArrayContains(this DbFunctions _, int[] array, int value)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool ArrayContains(this DbFunctions _, List<int> array, int value)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool ArrayContains(this DbFunctions _, int[] array, int[] contained)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool ArrayOverlaps(this DbFunctions _, int[] left, int[] right)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static int ArrayLength(this DbFunctions _, int[] array)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static int ArrayLength(this DbFunctions _, List<int> array)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool RangeContains(this DbFunctions _, KdbndpRange<int> range, int value)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool RangeContains(this DbFunctions _, KdbndpRange<int> range, KdbndpRange<int> contained)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool RangeOverlaps(this DbFunctions _, KdbndpRange<int> left, KdbndpRange<int> right)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool FullTextMatches(this DbFunctions _, KdbndpTsVector vector, string query)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");

    public static bool FullTextMatches(this DbFunctions _, KdbndpTsVector vector, string configuration, string query)
        => throw new InvalidOperationException("This KingbaseES function can only be used in LINQ queries.");
}
