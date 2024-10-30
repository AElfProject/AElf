using System.Text.Json;
using AElf;
using Google.Protobuf;

namespace Scale;

public class VecType<T> : PrimitiveType<T[]> where T : IScaleType, new()
{
    public override string TypeName => $"vec<{new T().TypeName}>";

    public static explicit operator VecType<T>(T[] p) => new(p);
    public static implicit operator T[](VecType<T> p) => p.Value;

    public VecType()
    {
    }

    public VecType(T[] value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return GetBytesFrom(Value);
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        var length = CompactIntegerType.Decode(byteArray, ref p);

        var array = new T[length];
        for (var i = 0; i < length; i++)
        {
            var t = new T();
            t.Decode(byteArray, ref p);
            array[i] = t;
        }

        TypeSize = p - start;

        Bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, Bytes, 0, TypeSize);
        Value = array;
    }

    public override void Create(T[] list)
    {
        Value = list;
        Bytes = Encode();
        TypeSize = Bytes.Length;
    }

    public void Create(byte[] byteArray)
    {
        var p = 0;
        Decode(byteArray, ref p);
    }

    public override string ToString() => JsonSerializer.Serialize(this);

    public static ByteString GetByteStringFrom(T[] list)
    {
        return ByteString.CopyFrom(GetBytesFrom(list));
    }

    public static byte[] GetBytesFrom(T[] list)
    {
        var result = new List<byte>();
        result.AddRange(new CompactIntegerType(list.Length).Encode());
        foreach (var element in list)
        {
            result.AddRange(element.Encode());
        }

        return result.ToArray();
    }

    public static VecType<T> From(T[] list)
    {
        return new VecType<T>(list);
    }

    public static VecType<T> From(byte[] value)
    {
        var instance = new VecType<T>();
        instance.Create(value);
        return instance;
    }

    public static VecType<T> From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}