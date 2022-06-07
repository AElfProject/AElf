using System.Collections.Generic;
using System.Diagnostics;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Type = System.Type;

namespace AElf.Kernel.Types.Tests;

public class SerializationTest
{
    [Fact]
    public void FromTo_Test()
    {
        var t = new Transaction();
        t.From = Address.FromBase58("nGmKp2ekysABSZAzVfXDrmaTNTaSSrfNmDhuaz7RUj5RTCYqy");
        t.To = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM");

        var b = t.ToByteArray();
        b.ShouldNotBe(null);
        b.Length.ShouldBeGreaterThan(0);

        var bstr = b.ToHex();
        bstr.ShouldNotBe(string.Empty);
    }


    [Fact]
    public void Deserialize_Test()
    {
        var bytes = ByteArrayHelper.HexStringToByteArray(
            "0a200a1e9dee15619106b96861d52f03ad30ac7e57aa529eb2f05f7796472d8ce4a112200a1e96d8bf2dccf2ad419d02ed4a7b7a9d77df10617c4d731e766ce8dde63535320a496e697469616c697a653a0a0a015b120122180020005003");
        var txBytes = ByteString.CopyFrom(bytes).ToByteArray();
        var txn = Transaction.Parser.ParseFrom(txBytes);
        var str = txn.From.Value.ToByteArray().ToHex();
    }

    [Fact]
    public void DefaultValue_Test()
    {
        Debug.WriteLine(default(UInt64Value));
    }
}

public enum ParamType
{
    String,
    Boolean,
    Single,
    Double,
    Int16,
    Int32,
    Int64,
    Byte,
    Decimal,
    UInt16,
    UInt32,
    UInt64,
    Bytes,
    Chars
}

public class SchemaGenerator
{
    private static readonly Dictionary<Type, ParamType> Maps = new()
    {
        { typeof(string), ParamType.String },
        { typeof(bool), ParamType.Boolean },
        { typeof(float), ParamType.Single },
        { typeof(double), ParamType.Double },
        { typeof(byte), ParamType.Byte },
        { typeof(decimal), ParamType.Decimal },
        { typeof(ushort), ParamType.UInt16 },
        { typeof(uint), ParamType.UInt32 },
        { typeof(ulong), ParamType.UInt64 },
        { typeof(short), ParamType.Int16 },
        { typeof(int), ParamType.Int32 },
        { typeof(long), ParamType.Int64 },
        { typeof(byte[]), ParamType.Bytes },
        { typeof(char[]), ParamType.Chars }
    };

    public static ParamType ToType(Type t)
    {
        return Maps[t];
    }
}