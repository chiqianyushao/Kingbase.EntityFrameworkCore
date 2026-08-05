namespace Kingbase.EntityFrameworkCore.Query.Internal;

public static class KingbaseByteArrayMethods
{
    public static bool SequenceEqual(byte[] left, byte[] right)
        => throw new InvalidOperationException("This method is for Kingbase SQL translation only.");
}
