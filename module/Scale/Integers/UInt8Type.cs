using System.Text.Json.Serialization;
using AElf;
using Google.Protobuf;

namespace Scale;

public class UInt8Type : PrimitiveType<byte>
{
    public override string TypeName => "uint8";

    [JsonIgnore] public override int TypeSize => 1;

    public static explicit operator UInt8Type(byte v) => new(v);

    public static implicit operator byte(UInt8Type v) => v.Value;
    public static implicit operator ByteString(UInt8Type v) => GetByteStringFrom(v.Value);

    public UInt8Type()
    {
    }

    public UInt8Type(byte value)
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
        Value = value[0];
    }

    public override void Create(byte value)
    {
        Bytes = [value];
        Value = value;
    }

    public static ByteString GetByteStringFrom(byte value)
    {
        return ByteString.CopyFrom(value);
    }

    public static byte[] GetBytesFrom(byte value)
    {
        return [value];
    }

    public static UInt8Type From(byte value)
    {
        var instance = new UInt8Type();
        instance.Create(value);
        return instance;
    }

    public static UInt8Type From(byte[] value)
    {
        var instance = new UInt8Type();
        instance.Create(value);
        return instance;
    }

    public static UInt8Type From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}