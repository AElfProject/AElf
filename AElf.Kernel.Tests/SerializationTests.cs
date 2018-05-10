using System;
using System.Collections.Generic;

namespace AElf.Kernel.Tests
{
    public class SerializationTests
    {
        /*
        [Fact]
        public void Test()
        {
            //object[] objs = new object[] {(int) 0, (byte) 1, false, "str"};
            //var bytes = ZeroFormatterSerializer.Serialize(objs);
            //object[] objsRevert = ZeroFormatterSerializer.Deserialize<object[]>(bytes);
            //Expression<> exp = (ISmartContractZero o) => o.GetSmartContractAsync(Hash<IAccount>.Zero);
        }

        [Fact]
        public void Test2()
        {

        }*/
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