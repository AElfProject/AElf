using System.Numerics;
using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class Int256Type : PrimitiveType<BigInteger>
{
    public override string TypeName => "int256";

    [JsonIgnore] public override int TypeSize => 32;

    public static explicit operator Int256Type(BigInteger v) => new(v);

    public static implicit operator BigInteger(Int256Type v) => v.Value;
    public static implicit operator ByteString(Int256Type v) => GetByteStringFrom(v.Value);

    public Int256Type()
    {
    }

    public Int256Type(BigInteger value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        if (value.Length < TypeSize)
        {
            var newByteArray = new byte[TypeSize];
            value.CopyTo(newByteArray, 0);
            value = newByteArray;
        }

        Bytes = value;
        Value = new BigInteger(value);
    }

    public void Create(long value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public override void Create(BigInteger value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(long value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static ByteString GetByteStringFrom(BigInteger value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(long value)
    {
        var bytes = new byte[32];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] GetBytesFrom(BigInteger value)
    {
        var byteArray = value.ToByteArray();
        if (byteArray.Length > 32)
        {
            throw new NotSupportedException("Exceeded the max size for int128.");
        }

        var bytes = new byte[32];
        byteArray.CopyTo(bytes, 0);
        return bytes;
    }

    public static Int256Type From(BigInteger value)
    {
        var instance = new Int256Type();
        instance.Create(value);
        return instance;
    }

    public static Int256Type From(byte[] value)
    {
        var instance = new Int256Type();
        instance.Create(value);
        return instance;
    }

    public static Int256Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}