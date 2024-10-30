using System.Numerics;
using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class UInt128Type : PrimitiveType<BigInteger>
{
    public override string TypeName => "uint128";

    [JsonIgnore] public override int TypeSize => 16;

    public static explicit operator UInt128Type(BigInteger v) => new(v);

    public static implicit operator BigInteger(UInt128Type v) => v.Value;
    public static implicit operator ByteString(UInt128Type v) => GetByteStringFrom(v.Value);

    public UInt128Type()
    {
    }

    public UInt128Type(BigInteger value)
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
        else if (value.Length == TypeSize)
        {
            var newArray = new byte[value.Length + 2];
            value.CopyTo(newArray, 0);
            newArray[value.Length - 1] = 0x00;
        }
        else
        {
            throw new NotSupportedException("Exceeded the max size for uint128.");
        }

        Bytes = value;
        Value = new BigInteger(value);
    }

    public void Create(ulong value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public override void Create(BigInteger value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(ulong value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static ByteString GetByteStringFrom(BigInteger value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(ulong value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] GetBytesFrom(BigInteger value)
    {
        if (value.Sign < 0)
        {
            throw new InvalidOperationException("Value of uint128 type cannot be negative");
        }

        var byteArray = value.ToByteArray();
        if (byteArray.Length > 16)
        {
            throw new NotSupportedException("Exceeded the max size for uint128.");
        }

        var bytes = new byte[16];
        byteArray.CopyTo(bytes, 0);
        return bytes;
    }

    public static UInt128Type From(BigInteger value)
    {
        var instance = new UInt128Type();
        instance.Create(value);
        return instance;
    }

    public static UInt128Type From(byte[] value)
    {
        var instance = new UInt128Type();
        instance.Create(value);
        return instance;
    }

    public static UInt128Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}