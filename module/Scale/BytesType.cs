using AElf;
using Google.Protobuf;

namespace Scale;

public class BytesType : PrimitiveType<byte[]>
{
    public override string TypeName => $"bytes{TypeSize}";

    public BytesType()
    {
    }

    public BytesType(byte[] value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        if (value.Length is < 1 or > 32)
        {
            throw new NotSupportedException("Bytes is a fixed-length byte array of 1 to 32 bytes");
        }

        TypeSize = value.Length;
        Bytes = value;
        Value = value;
    }

    public static ByteString GetByteStringFrom(byte[] value)
    {
        return ByteString.CopyFrom(value);
    }

    public static BytesType From(byte[] value)
    {
        var instance = new BytesType();
        instance.Create(value);
        return instance;
    }

    public static BytesType From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}