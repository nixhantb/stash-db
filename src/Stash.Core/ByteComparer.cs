namespace Stash.Core;

public sealed class ByteComparer : IComparer<byte[]>
{
    public int Compare(byte[]? x, byte[]? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        int length=Math.Min(x.Length, y.Length);
        
        for(int i=0; i < length; i++)
        {
            if(x[i] < y[i]) return -1;
            if(x[i] > y[i]) return 1;
        }
        return x.Length.CompareTo(y.Length);
    }
}