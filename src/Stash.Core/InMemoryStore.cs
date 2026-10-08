
namespace Stash.Core;

public sealed class InMemoryStore : IStore
{
    private readonly SortedDictionary<byte[], byte[]> _data = new(new ByteComparer());
    private readonly ByteComparer _byteComparer = new();
    public bool Delete(byte[] key)
    {
        return _data.Remove(key);
    }

    public bool Get(byte[] key, out byte[] value)
    {
        return _data.TryGetValue(key, out value!);
    }

    public void Put(byte[] key, byte[] value)
    {
        _data[(byte[])key.Clone()] = (byte[])value.Clone();
    }

    public IEnumerable<KeyValuePair<byte[], byte[]>> Scan(byte[]? from, byte[]? to)
    {
        foreach(var pair in _data)
        {
            bool lowerBound = from is null || _byteComparer.Compare(pair.Key, from) >=0;
            bool upperBound = to is null || _byteComparer.Compare(pair.Key, to) < 0;

            if(lowerBound && upperBound) yield return pair;
        }
    }
}