using Xunit;

namespace Stash.Core.Tests;

public class InMemoryStoreTests
{
    static byte[] B(params byte[] v)
    {
        return v;
    }

    static string Hex(byte[] b)
    {
        return Convert.ToHexString(b);
    }

    static string[] Keys(IEnumerable<KeyValuePair<byte[], byte[]>> scan)
    {
        var list = new List<string>();
        foreach (var pair in scan) list.Add(Hex(pair.Key));
        return list.ToArray();
    }

    static int Compare(byte[] a, byte[] b)
    {
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            if (a[i] != b[i]) return a[i] < b[i] ? -1 : 1;
        }
        return a.Length.CompareTo(b.Length);
    }
    
    
    [Fact]
    public void Ordering_Unsigned_Lexicographic_PrefixFirst()
    {
        
        // 0x80 after 0x7F catches signed bytes. [1,0,0] before [2] catches "shorter first".
        /* ""    → []
        "00"     → [0]
        "0000"   → [0,0]

        "01"     → [1]
        "0100"   → [1,0]
        "010000" → [1,0,0]
        "01FF"   → [1,255]

        "02"     → [2]

        "7F"     → [127]
        "80"     → [128]
        "FF"     → [255]
        */
        // Signed bytes: -128 to 127 (We don't want)
        // Unsigned bytes: 0 to 255 (We want this)
        // Since we are using unsigned bytes, a byte can have a value from 0 to 255.
        // If we used signed bytes, 0x7F would be 127 but 0x80 would be -128.
        // This would cause incorrect ordering because -128 comes before 127.
        // Our store should treat 0x7F as 127 and 0x80 as 128,
        // so the correct order is 0x7F (127) before 0x80 (128).
        // We don't want negative values when comparing our byte keys.

        var store = new InMemoryStore();
      
        foreach (var k in new[] { B(0xFF), B(0), B(), B(0x80), B(0x7F), B(1, 0), B(1), B(0, 0), B(2), B(1, 0xFF), B(1, 0, 0) })
            store.Put(k, B(0));
 
        Assert.Equal(new[] { "", "00", "0000", "01", "0100", "010000", "01FF", "02", "7F", "80", "FF" },
            Keys(store.Scan(null, null)));
    }


    [Fact]
    public void Scan_RangeEdgeCases()
    {
        var store = new InMemoryStore();
        foreach (var k in new[] { B(), B(1), B(1, 0), B(1, 255), B(2), B(2, 0), B(3) }) store.Put(k, B(0));
 
        Assert.Equal(new[] { "01", "0100", "01FF" }, Keys(store.Scan(B(1), B(2))));       // from in, to out
        Assert.Equal(new[] { "01FF", "02" }, Keys(store.Scan(B(1, 1), B(2, 0))));  // bounds not stored
        Assert.Empty(store.Scan(B(1), B(1)));                                              // from == to
        Assert.Empty(store.Scan(B(3), B(1)));                                              // from > to
        Assert.Empty(store.Scan(null, B()));                                               // nothing below []
    }



    [Theory]
    [InlineData(1)] [InlineData(42)] [InlineData(1337)]
    public void RandomOps_MatchSimpleModel(int seed)

    {
        var alphabet = new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF };  // 0 1 127 128 255
        var random = new Random(seed); 
        var store = new InMemoryStore();
        var model = new Dictionary<string, byte[]>();
 
        for (int step = 0; step < 5000; step++)
        {
            var key = new byte[random.Next(0, 4)]; // key = 0, 1 , 2 or 3
            for (int i = 0; i < key.Length; i++) key[i] = alphabet[random.Next(alphabet.Length)];
            int op = random.Next(5);
 
            if (op <= 1)
            {
                var value = B((byte)random.Next(256));
                store.Put((byte[])key.Clone(), value);
                model[Hex(key)] = value;
            }
            else if (op == 2)
            {
                Assert.Equal(model.Remove(Hex(key)), store.Delete(key));
            }
            else if (op == 3)
            {
                bool found = model.TryGetValue(Hex(key), out var expected);
                Assert.Equal(found, store.Get(key, out var actual));
                if (found) Assert.Equal(expected, actual);
            }
            else
            {
                // scan from this key up to the end: work out the expected answer the slow way
                var expectedKeys = new List<byte[]>();
                foreach (var hex in model.Keys)
                {
                    var k = Convert.FromHexString(hex);
                    if (Compare(k, key) >= 0) expectedKeys.Add(k);
                }
                expectedKeys.Sort(Compare);
                var expectedHex = new List<string>();
                foreach (var k in expectedKeys) expectedHex.Add(Hex(k));
 
                Assert.Equal(expectedHex.ToArray(), Keys(store.Scan(key, null)));
            }
        }
    }

    [Fact]
    public void Put_CallerChangesKeyOrValueAfterwards_StoreMustNotChange()
    {
        //ownership / defensive copying test
        var store = new InMemoryStore();
        var key = B(5);
        var value = B(1, 2, 3);

        store.Put(B(1), B(0)); // [001 -> 00]
        store.Put(key, value); // [005 -> 001, 002, 003]
        store.Put(B(9), B(0));// [009 -> 00]
        key[0] = 0;      
        value[0] = 99;
 
        Assert.True(store.Get(B(5), out var found));
        Assert.Equal(B(1, 2, 3), found);
        Assert.Equal(new[] { "01", "05", "09" }, Keys(store.Scan(null, null)));
    }
    [Fact]
    public void ReturnedArrays_ChangingThemMustNotChangeStore()
    {
        var store = new InMemoryStore();
        store.Put(B(1), B(1, 2, 3));
        store.Put(B(2), B(0));
 
        store.Get(B(1), out var returned); // returned = (1, 2, 3)
        returned[0] = 99; // returned => (0, 2, 3)
        // keys: 01 , 02
        foreach (var pair in store.Scan(null, null)) pair.Key[0] = 0xFF;
 
        store.Get(B(1), out var again);
        Assert.Equal(B(1, 2, 3), again);
        Assert.Equal(new[] { "01", "02" }, Keys(store.Scan(null, null)));
    }

}