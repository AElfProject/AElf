using System.Text;
using Google.Protobuf;

namespace Scale;

public class StringType : VecType<UInt8Type>
{
    public override string TypeName => "string";

    public static explicit operator StringType(string p) => new(p);
    public static implicit operator string(StringType p) => Encoding.UTF8.GetString(p.Encode());

    public StringType()
    {
    }

    public StringType(string value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return GetBytesFrom(Value);
    }

    public override void Create(string value)
    {
        Value = Encoding.UTF8.GetBytes(value).Select(b => new UInt8Type(b)).ToArray();
        Bytes = GetBytesFrom(value);
        TypeSize = Bytes.Length;
    }

    public static ByteString GetByteStringFrom(string value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(string value)
    {
        var list = Encoding.UTF8.GetBytes(value).ToList();
        var result = new List<byte>();
        result.AddRange(new CompactIntegerType(list.Count).Encode());
        result.AddRange(list);
        return result.ToArray();
    }

    public static StringType From(byte[] value)
    {
        var instance = new StringType();
        instance.Create(value);
        return instance;
    }

    public static StringType From(string value)
    {
        var instance = new StringType();
        instance.Create(value);
        return instance;
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Value.Select(v => v.Value).ToArray());
    }
}