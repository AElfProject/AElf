using System.Text;

namespace Scale.Encoders;

public class StringTypeEncoder
{
    public byte[] Encode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var length = new CompactIntegerTypeEncoder().Encode((ulong)bytes.Length);
        return length.Concat(bytes).ToArray();
    }
}