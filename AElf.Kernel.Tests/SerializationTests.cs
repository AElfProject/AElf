using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel.Types.Proposal;
using Akka.Actor;
using Google.Protobuf;
using Xunit;
using Address = AElf.Common.Address;

namespace AElf.Kernel.Tests
{
    public class SerializationTests
    {
        
        [Fact]
        public void Test()
        {
            string authsth =
                "0a140a12100222450a41049998999fcdba044bdd81a6100222450a41049998999fcdba044bdd81a6d87de60f12d55c200bdd93b3c011d1ce4c916321439f432ea58eb79c4c4b3011231b57ec21fe086ae1bd67519cce2b85207d49270e100122450a410479dff263e4af1ea7aced5a5944c8d1b7cadda1019c869a650df9433e32b4b6bebe98d2c41b1150559111ceec86830d45f69b508e531b53ae0e759d83bab770c91001";
            var auth = Authorization.Parser.ParseFrom(ByteArrayHelpers.FromHexString(authsth));
            
            string str = "0a140a12100222450a41049998999fcdba044bdd81a6120f496e697469616c697a65546f6b656e1a610a0f496e697469616c697a65546f6b656e124e0a140a12100222450a41049998999fcdba044bdd81a612140a1299496b786bb418c4211a1ff7535b2d9d81a3320a496e697469616c697a653a120a0461656c66120441456c6618a08d0620015003220c0887f682e00510988283ea01";
            var proposal = Proposal.Parser.ParseFrom(ByteArrayHelpers.FromHexString(str));
            var txData = proposal.TxnData;
            var txn = Transaction.Parser.ParseFrom(txData.TxnData);
            ;
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