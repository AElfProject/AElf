using System.Text.Json;
using System.Text.Json.Serialization;
using AElf;

namespace Scale;

public abstract class BaseType : IScaleType
{
    public virtual string TypeName => GetType().Name;

    [JsonIgnore] public virtual int TypeSize { get; internal set; }

    [JsonIgnore] public byte[] Bytes { get; set; }

    public abstract byte[] Encode();

    public abstract void Decode(byte[] byteArray, ref int p);

    public virtual void Create(string value) => Create(ByteArrayHelper.HexStringToByteArray(value));

    public virtual void Create(byte[] value)
    {
        var p = 0;
        Bytes = value;
        Decode(value, ref p);
    }

    public override string ToString() => JsonSerializer.Serialize(this);
}