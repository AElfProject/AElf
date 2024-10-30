using Google.Protobuf;

namespace Scale;

public class TupleType : BaseType
{
    public override string TypeName => "()";

    public override byte[] Encode()
    {
        return [];
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        TypeSize = 0;
        Bytes = [];
    }

    public static ByteString GetByteStringFrom(params ByteString[] values)
    {
        using var memoryStream = new MemoryStream();
        foreach (var value in values)
        {
            var byteArray = value.ToByteArray();
            memoryStream.Write(byteArray, 0, byteArray.Length);
        }

        return ByteString.CopyFrom(memoryStream.ToArray());
    }
}

public class TupleType<T1> : BaseType
    where T1 : IScaleType, new()
{
    public override string TypeName => $"({new T1().TypeName})";

    public IScaleType[] Value { get; internal set; }

    public TupleType()
    {
    }

    public TupleType(T1 t1)
    {
        Create(t1);
    }

    public override byte[] Encode()
    {
        var result = new List<byte>();
        foreach (var v in Value)
        {
            result.AddRange(v.Encode());
        }

        return result.ToArray();
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        Value = new IScaleType[1];

        var t1 = new T1();
        t1.Decode(byteArray, ref p);
        Value[0] = t1;
        TypeSize = p - start;

        Bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, Bytes, 0, TypeSize);
    }

    public void Create(T1 t1)
    {
        var byteList = new List<byte>();
        byteList.AddRange(t1.Encode());
        byteList.ToArray();

        Value = new IScaleType[1];
        Value[0] = t1;

        Bytes = byteList.ToArray();
        TypeSize = Bytes.Length;
    }

    public static ByteString GetByteStringFrom(T1 t1)
    {
        return ByteString.CopyFrom(t1.Encode());
    }

    public static byte[] GetBytesFrom(T1 t1)
    {
        return t1.Encode();
    }

    public static TupleType<T1> From(T1 t1)
    {
        var instance = new TupleType<T1>();
        instance.Create(t1);
        return instance;
    }
}

public class TupleType<T1, T2> : BaseType
    where T1 : IScaleType, new()
    where T2 : IScaleType, new()
{
    public override string TypeName => $"({new T1().TypeName}, {new T2().TypeName})";

    public IScaleType[] Value { get; internal set; }

    public TupleType()
    {
    }

    public TupleType(T1 t1, T2 t2)
    {
        Create(t1, t2);
    }

    public override byte[] Encode()
    {
        var result = new List<byte>();
        foreach (var v in Value)
        {
            result.AddRange(v.Encode());
        }

        return result.ToArray();
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        Value = new IScaleType[2];

        var t1 = new T1();
        t1.Decode(byteArray, ref p);
        Value[0] = t1;
        var t2 = new T2();
        t2.Decode(byteArray, ref p);
        Value[1] = t2;
        TypeSize = p - start;

        Bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, Bytes, 0, TypeSize);
    }

    public void Create(T1 t1, T2 t2)
    {
        var byteList = new List<byte>();
        byteList.AddRange(t1.Encode());
        byteList.AddRange(t2.Encode());

        Value = new IScaleType[2];
        Value[0] = t1;
        Value[1] = t2;

        Bytes = byteList.ToArray();
        TypeSize = Bytes.Length;
    }

    public void Create(byte[] byteArray)
    {
        var offset = 0;
        offset += new T1().TypeSize;
        var t1 = new T1();
        t1.Create(byteArray.Take(offset).ToArray());

        var t2 = new T2();
        t2.Create(byteArray.Skip(offset).ToArray());

        Value[0] = t1;
        Value[1] = t2;

        Bytes = byteArray;
        TypeSize = byteArray.Length;
    }

    public static ByteString GetByteStringFrom(T1 t1, T2 t2)
    {
        return ByteString.CopyFrom(GetBytesFrom(t1, t2));
    }

    public static byte[] GetBytesFrom(T1 t1, T2 t2)
    {
        return t1.Encode().Concat(t2.Encode()).ToArray();
    }

    public static TupleType<T1, T2> From(T1 t1, T2 t2)
    {
        var instance = new TupleType<T1, T2>();
        instance.Create(t1, t2);
        return instance;
    }
    
    public static TupleType<T1, T2> From(byte[] byteArray)
    {
        var instance = new TupleType<T1, T2>();
        var p = 0;
        instance.Decode(byteArray, ref p);
        return instance;
    }
}

public class TupleType<T1, T2, T3> : BaseType
    where T1 : IScaleType, new()
    where T2 : IScaleType, new()
    where T3 : IScaleType, new()
{
    public override string TypeName => $"({new T1().TypeName}, {new T2().TypeName}, {new T3().TypeName})";

    public IScaleType[] Value { get; internal set; }

    public TupleType()
    {
    }

    public TupleType(T1 t1, T2 t2, T3 t3)
    {
        Create(t1, t2, t3);
    }

    public override byte[] Encode()
    {
        var result = new List<byte>();
        foreach (var v in Value)
        {
            result.AddRange(v.Encode());
        }

        return result.ToArray();
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        Value = new IScaleType[3];

        var t1 = new T1();
        t1.Decode(byteArray, ref p);
        Value[0] = t1;
        var t2 = new T2();
        t2.Decode(byteArray, ref p);
        Value[1] = t2;
        TypeSize = p - start;
        var t3 = new T3();
        t3.Decode(byteArray, ref p);
        Value[2] = t3;
        TypeSize = p - start;

        Bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, Bytes, 0, TypeSize);
    }

    public void Create(T1 t1, T2 t2, T3 t3)
    {
        var byteList = new List<byte>();
        byteList.AddRange(t1.Encode());
        byteList.AddRange(t2.Encode());
        byteList.AddRange(t3.Encode());

        Value = new IScaleType[3];
        Value[0] = t1;
        Value[1] = t2;
        Value[2] = t3;

        Bytes = byteList.ToArray();
        TypeSize = Bytes.Length;
    }

    public void Create(byte[] byteArray)
    {
        var offset = 0;
        offset += new T1().TypeSize;
        var t1 = new T1();
        t1.Create(byteArray.Take(offset).ToArray());

        var t2 = new T2();
        t2.Create(byteArray.Skip(offset).ToArray());

        var t3 = new T3();
        t3.Create(byteArray.Skip(offset).ToArray());

        Value[0] = t1;
        Value[1] = t2;
        Value[2] = t3;

        Bytes = byteArray;
        TypeSize = byteArray.Length;
    }

    public static ByteString GetByteStringFrom(T1 t1, T2 t2, T3 t3)
    {
        return ByteString.CopyFrom(GetBytesFrom(t1, t2, t3));
    }

    public static byte[] GetBytesFrom(T1 t1, T2 t2, T3 t3)
    {
        return t1.Encode().Concat(t2.Encode()).Concat(t3.Encode()).ToArray();
    }

    public static TupleType<T1, T2, T3> From(T1 t1, T2 t2, T3 t3)
    {
        var instance = new TupleType<T1, T2, T3>();
        instance.Create(t1, t2, t3);
        return instance;
    }
    
    public static TupleType<T1, T2, T3> From(byte[] byteArray)
    {
        var instance = new TupleType<T1, T2, T3>();
        var p = 0;
        instance.Decode(byteArray, ref p);
        return instance;
    }
}

public class TupleType<T1, T2, T3, T4> : BaseType
    where T1 : IScaleType, new()
    where T2 : IScaleType, new()
    where T3 : IScaleType, new()
    where T4 : IScaleType, new()
{
    public override string TypeName =>
        $"({new T1().TypeName}, {new T2().TypeName}, {new T3().TypeName}, {new T4().TypeName})";

    public IScaleType[] Value { get; internal set; }

    public TupleType()
    {
    }

    public TupleType(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        Create(t1, t2, t3, t4);
    }

    public override byte[] Encode()
    {
        var result = new List<byte>();
        foreach (var v in Value)
        {
            result.AddRange(v.Encode());
        }

        return result.ToArray();
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        Value = new IScaleType[4];

        var t1 = new T1();
        t1.Decode(byteArray, ref p);
        Value[0] = t1;
        var t2 = new T2();
        t2.Decode(byteArray, ref p);
        Value[1] = t2;
        TypeSize = p - start;
        var t3 = new T3();
        t3.Decode(byteArray, ref p);
        Value[2] = t3;
        TypeSize = p - start;
        var t4 = new T4();
        t4.Decode(byteArray, ref p);
        Value[3] = t4;
        TypeSize = p - start;

        Bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, Bytes, 0, TypeSize);
    }

    public void Create(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        var byteList = new List<byte>();
        byteList.AddRange(t1.Encode());
        byteList.AddRange(t2.Encode());
        byteList.AddRange(t3.Encode());
        byteList.AddRange(t4.Encode());

        Value = new IScaleType[4];
        Value[0] = t1;
        Value[1] = t2;
        Value[2] = t3;
        Value[3] = t3;

        Bytes = byteList.ToArray();
        TypeSize = Bytes.Length;
    }

    public void Create(byte[] byteArray)
    {
        var offset = 0;
        offset += new T1().TypeSize;
        var t1 = new T1();
        t1.Create(byteArray.Take(offset).ToArray());

        var t2 = new T2();
        t2.Create(byteArray.Skip(offset).ToArray());

        var t3 = new T3();
        t3.Create(byteArray.Skip(offset).ToArray());

        var t4 = new T4();
        t4.Create(byteArray.Skip(offset).ToArray());

        Value[0] = t1;
        Value[1] = t2;
        Value[2] = t3;
        Value[3] = t4;

        Bytes = byteArray;
        TypeSize = byteArray.Length;
    }

    public static ByteString GetByteStringFrom(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        return ByteString.CopyFrom(GetBytesFrom(t1, t2, t3, t4));
    }

    public static byte[] GetBytesFrom(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        return t1.Encode().Concat(t2.Encode()).Concat(t3.Encode()).Concat(t4.Encode()).ToArray();
    }

    public static TupleType<T1, T2, T3, T4> From(T1 t1, T2 t2, T3 t3, T4 t4)
    {
        var instance = new TupleType<T1, T2, T3, T4>();
        instance.Create(t1, t2, t3, t4);
        return instance;
    }

    public static TupleType<T1, T2, T3, T4> From(byte[] byteArray)
    {
        var instance = new TupleType<T1, T2, T3, T4>();
        var p = 0;
        instance.Decode(byteArray, ref p);
        return instance;
    }
}