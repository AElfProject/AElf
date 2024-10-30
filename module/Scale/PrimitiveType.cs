using System.Text.Json.Serialization;
using Google.Protobuf;

namespace Scale;

public abstract class PrimitiveType<T> : BaseType
{
    public T Value { get; set; }
    [JsonIgnore] public ByteString ByteStringValue => ByteString.CopyFrom(Bytes);

    public abstract void Create(T value);

    public override void Decode(byte[] byteArray, ref int p)
    {
        var memory = byteArray.AsMemory();
        var result = memory.Span.Slice(p, TypeSize).ToArray();
        p += TypeSize;
        Create(result);
    }

    public override string ToString() => Value.ToString();
}