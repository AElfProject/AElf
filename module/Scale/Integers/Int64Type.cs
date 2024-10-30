using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class Int64Type : PrimitiveType<long>
{
    public override string TypeName => "int64";

    [JsonIgnore] public override int TypeSize => 8;

    public static explicit operator Int64Type(long v) => new(v);

    public static implicit operator long(Int64Type v) => v.Value;
    public static implicit operator ByteString(Int64Type v) => GetByteStringFrom(v.Value);

    public Int64Type()
    {
    }

    public Int64Type(long value)
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
        Value = BitConverter.ToInt64(value, 0);
    }

    public override void Create(long value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString GetByteStringFrom(long value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(long value)
    {
        var bytes = new byte[8];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static Int64Type From(long value)
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