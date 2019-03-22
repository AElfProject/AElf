using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    // Lot of types data should not be defined including: byte, sbyte, char, short, ushort, decimal, float and double. 
    // Becasue ParamsPacker.Pack() those types data wil be throw exception.
     
    public class TransactionEvent : Event
    {
        [Indexed] public Transaction IndexTransaction { get; set; }
        public Transaction NonIndexTransaction { get; set; }
    }

    public class AddressEvent : Event
    {
        [Indexed] public Address IndexAddress { get; set; }
        public Address NonIndexAddress { get; set; }
    }

    public class StringEvent : Event
    {
        [Indexed] public string IndexString { get; set; }
        public string NonIndexString { get; set; }
    }

    public class DataEvent : Event
    {
        [Indexed] public int IndexInt { get; set; }
        [Indexed] public uint IndexUInt {get;set;}
        [Indexed] public long IndexLong {get;set;}
        [Indexed] public ulong IndexULong {get;set;}

        public int NonIndexInt { get; set; }
        public long NonIndexLong {get;set;}
    }

    public class MultiEvent : Event
    {
        [Indexed] public Transaction IndexTransaction {get;set;}
        [Indexed] public Address IndexAddress {get;set;}
        [Indexed] public string IndexString {get;set;}
        [Indexed] public int IndexInt {get;set;}
        [Indexed] public long IndexLong {get;set;}

        public Address NonIndexAddress {get;set;}
        public string NonIndexString {get;set;}
        public int NonIndexInt {get;set;}
    }

    public class NotSupportEvent : Event
    {
        [Indexed] public double IndexDouble {get;set;}
        [Indexed] public float IndexFloat {get;set;}

        public double NonIndexDouble {get;set;}
        public float NonIndexFloat {get;set;}
    }
}
