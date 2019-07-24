using System;
using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Type = System.Type;

namespace AElf.Kernel.Types.Tests
{
    public class SerializationTest
    {
        [Fact]
        public void FromTo()
        {
            var t = new Transaction();
            t.From = AddressHelper.Base58StringToAddress("nGmKp2ekysABSZAzVfXDrmaTNTaSSrfNmDhuaz7RUj5RTCYqy");
            t.To = AddressHelper.Base58StringToAddress("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM");

            byte[] b = t.ToByteArray();
            b.ShouldNotBe(null);
            b.Length.ShouldBeGreaterThan(0);

            string bstr = b.ToHex();
            bstr.ShouldNotBe(string.Empty);

        }


        [Fact]
        public void Deserialize()
        {
            var bytes = ByteArrayHelper.HexStringToByteArray(
                "0a200a1e9dee15619106b96861d52f03ad30ac7e57aa529eb2f05f7796472d8ce4a112200a1e96d8bf2dccf2ad419d02ed4a7b7a9d77df10617c4d731e766ce8dde63535320a496e697469616c697a653a0a0a015b120122180020005003");
            var txBytes = ByteString.CopyFrom(bytes).ToByteArray();
            var txn = Transaction.Parser.ParseFrom(txBytes);
            string str =txn.From.Value.ToByteArray().ToHex();
        }

        [Fact]
        public void DefaultValueTest()
        {
            System.Diagnostics.Debug.WriteLine(default(UInt64Value));
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
        private static readonly Dictionary<Type, ParamType> Maps = new Dictionary<Type, ParamType>(
        )
        {
            {typeof(String), ParamType.String},
            {typeof(Boolean), ParamType.Boolean},
            {typeof(Single), ParamType.Single},
            {typeof(Double), ParamType.Double},
            {typeof(Byte), ParamType.Byte},
            {typeof(Decimal), ParamType.Decimal},
            {typeof(UInt16), ParamType.UInt16},
            {typeof(UInt32), ParamType.UInt32},
            {typeof(UInt64), ParamType.UInt64},
            {typeof(Int16), ParamType.Int16},
            {typeof(Int32), ParamType.Int32},
            {typeof(Int64), ParamType.Int64},
            {typeof(byte[]), ParamType.Bytes},
            {typeof(char[]), ParamType.Chars},
        };

        public static ParamType ToType(Type t)
        {
            return Maps[t];
        }
    }


}