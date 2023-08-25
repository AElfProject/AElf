namespace AElf.Runtime.WebAssembly.Tests;

public class ByteArrayBuilder
{
    public byte[] RepeatedBytes(byte repeatedByte, int count)
    {
        var bytes = new byte[count];
        for (var i = 0; i < count; i++)
        {
            bytes[i] = repeatedByte;
        }

        return bytes;
    }
}