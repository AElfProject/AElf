namespace Scale.Core;

public static class ByteExtensions
{
    public static byte[] RightPad(this byte[] bytes, int length)
    {
        if (length <= bytes.Length)
            return bytes;

        var paddedBytes = new byte[length];
        Buffer.BlockCopy(bytes, 0, paddedBytes, 0, bytes.Length);
        return paddedBytes;
    }
}