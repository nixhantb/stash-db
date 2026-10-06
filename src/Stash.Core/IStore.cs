namespace Stash.Core;

/// <summary>
/// IStore is an store interface with Get, Put, Delete
/// and Scan method definition.
/// key and values are stored in bytes 
/// </summary>
public interface IStore
{
    bool Get(byte[] key, out byte[] value);
    void Put(byte[] key, byte[] value);
    bool Delete(byte[] key);
    IEnumerable<KeyValuePair<byte[],byte[]>> Scan(byte[]? from, byte[]? to);
}