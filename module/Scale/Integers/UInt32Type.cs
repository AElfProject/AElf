using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class UInt32Type : PrimitiveType<uint>
{
    public override string TypeName => "uint32";

    [JsonIgnore] public override int TypeSize => 4;

    public static explicit operator UInt32Type(uint v) => new(v);

    public static implicit operator uint(UInt32Type v) => v.Value;
    public static implicit operator ByteString(UInt32Type v) => GetByteStringFrom(v.Value);

    public UInt32Type()
    {
    }

    public UInt32Type(uint value)
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
        Value = BitConverter.ToUInt32(value, 0);
    }

    public override void Create(uint value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(uint value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(uint value)
    {
        var bytes = new byte[4];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static UInt32Type From(uint value)
    {
        var instance = new UInt32Type();
        instance.Create(value);
        return instance;
    }

    public static UInt32Type From(byte[] value)
    {
        var instance = new UInt32Type();
        instance.Create(value);
        return instance;
    }

    public static UInt32Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}