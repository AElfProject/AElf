using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class Int8Type : PrimitiveType<sbyte>
{
    public override string TypeName => "int8";

    [JsonIgnore] public override int TypeSize => 1;

    public static explicit operator Int8Type(sbyte v) => new(v);

    public static implicit operator sbyte(Int8Type v) => v.Value;
    public static implicit operator ByteString(Int8Type v) => GetByteStringFrom(v.Value);

    public Int8Type()
    {
    }

    public Int8Type(sbyte value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = (sbyte)value[0];
    }

    public override void Create(sbyte value)
    {
        Bytes = [(byte)value];
        Value = value;
    }

    public static ByteString GetByteStringFrom(sbyte value)
    {
        return ByteString.CopyFrom((byte)value);
    }

    public static Int8Type From(sbyte value)
    {
        var instance = new Int8Type();
        instance.Create(value);
        return instance;
    }

    public static Int8Type From(byte[] value)
    {
        var instance = new Int8Type();
        instance.Create(value);
        return instance;
    }

    public static Int8Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}