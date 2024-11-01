using System.Text.Json.Serialization;
using AElf.Types;
using Google.Protobuf;
using Scale.Encoders;

namespace Scale;

/// <summary>
/// For aelf address.
/// </summary>
public class AddressType : PrimitiveType<byte[]>
{
    public Address Value { get; set; }

    public static ByteString GetByteStringFromBase58(string address)
    {
        return ByteString.CopyFrom(GetBytesFromBase58(address));
    }

    public static ByteString GetByteStringFrom(Address address)
    {
        return ByteString.CopyFrom(address.ToByteArray());
    }

    public static byte[] GetBytesFromBase58(string address)
    {
        return new AddressTypeEncoder().Encode(address);
    }

    public AddressType()
    {
    }

    public AddressType(Address address)
    {
        Value = address;
        Bytes = address.ToByteArray();
    }

    public AddressType(byte[] bytes)
    {
        Create(bytes);
    }

    public override string TypeName => "address";
    [JsonIgnore] public override int TypeSize => 32;

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var memory = byteArray.AsMemory();
        var result = memory.Span.Slice(p, TypeSize).ToArray();
        p += TypeSize;
        Create(result);
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = Address.FromBytes(value);
    }

    public static AddressType From(byte[] value)
    {
        var instance = new AddressType();
        instance.Create(value);
        return instance;
    }
}