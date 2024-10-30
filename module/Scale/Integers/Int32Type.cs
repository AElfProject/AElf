using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class Int32Type : PrimitiveType<int>
{
    public override string TypeName => "int32";

    [JsonIgnore] public override int TypeSize => 4;

    public static explicit operator Int32Type(int v) => new(v);

    public static implicit operator int(Int32Type v) => v.Value;
    public static implicit operator ByteString(Int32Type v) => GetByteStringFrom(v.Value);

    public Int32Type()
    {
    }

    public Int32Type(int value)
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
        Value = BitConverter.ToInt32(value, 0);
    }

    public override void Create(int value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(int value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(int value)
    {
        var bytes = new byte[4];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static Int32Type From(int value)
    {
        var instance = new Int32Type();
        instance.Create(value);
        return instance;
    }

    public static Int32Type From(byte[] value)
    {
        var instance = new Int32Type();
        instance.Create(value);
        return instance;
    }

    public static Int32Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}