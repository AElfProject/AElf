using System.Numerics;
using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class UInt256Type : PrimitiveType<BigInteger>
{
    public override string TypeName => "uint256";

    [JsonIgnore] public override int TypeSize => 32;

    public static explicit operator UInt256Type(BigInteger v) => new(v);

    public static implicit operator BigInteger(UInt256Type v) => v.Value;
    public static implicit operator ByteString(UInt256Type v) => GetByteStringFrom(v.Value);

    public UInt256Type()
    {
    }

    public UInt256Type(BigInteger value)
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
            throw new NotSupportedException("Exceeded the max size for uint256.");
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

    public static UInt256Type FromBytes(byte[] bytes)
    {
        var instance = new UInt256Type();
        instance.Create(bytes);
        return instance;
    }

    public static byte[] GetBytesFrom(ulong value)
    {
        var bytes = new byte[32];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] GetBytesFrom(BigInteger value)
    {
        if (value.Sign < 0)
        {
            throw new InvalidOperationException("Value of uint256 type cannot be negative");
        }

        var byteArray = value.ToByteArray();
        if (byteArray.Length > 32)
        {
            throw new NotSupportedException("Exceeded the max size for uint256.");
        }

        var bytes = new byte[32];
        byteArray.CopyTo(bytes, 0);
        return bytes;
    }

    public static UInt256Type From(BigInteger value)
    {
        var instance = new UInt256Type();
        instance.Create(value);
        return instance;
    }

    public static UInt256Type From(byte[] value)
    {
        var instance = new UInt256Type();
        instance.Create(value);
        return instance;
    }

    public static UInt256Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}