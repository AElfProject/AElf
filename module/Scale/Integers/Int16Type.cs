using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class Int16Type : PrimitiveType<short>
{
    public override string TypeName => "int16";

    [JsonIgnore] public override int TypeSize => 2;

    public static explicit operator Int16Type(short v) => new(v);

    public static implicit operator short(Int16Type v) => v.Value;
    public static implicit operator ByteString(Int16Type v) => GetByteStringFrom(v.Value);

    public Int16Type()
    {
    }

    public Int16Type(short value)
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
        Value = BitConverter.ToInt16(value, 0);
    }

    public override void Create(short value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(short value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(short value)
    {
        var bytes = new byte[2];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static Int64Type From(short value)
    {
        var instance = new Int64Type();
        instance.Create(value);
        return instance;
    }

    public static Int64Type From(byte[] value)
    {
        var instance = new Int64Type();
        instance.Create(value);
        return instance;
    }

    public static Int64Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}