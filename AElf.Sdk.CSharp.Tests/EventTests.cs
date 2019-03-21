using System.Security.Cryptography;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class EventTests
    {
        // Lot of types data should not be defined including: byte, sbyte, char, short, ushort, decimal, float and double.
        [Fact]
        public void Parse_EventTest()
        {
            var eventDemo = new EventDemo()
            {
               //init index value
               IndexAddress = Address.Generate(),
               IndexSymbol = "ELF",
               //IndexByte = 125,
               //IndexSbyte = 100,
               //IndexChar = 'a',
               //IndexShort = 3,
               //IndexUShort = 4,
               IndexInt = 5,
               IndexUInt = 6,
               IndexLong = 10,
               IndexULong = 20,
               //IndexDecimal = new decimal(2.45),
               //IndexFloat = 128,
               //IndexDouble = 3.6,
               
               //init non index value
               Address = Address.Generate(),
               Symbol = "ELF",
               //Byte = 99,
               //Sbyte = 60,
               //Char = 'a',
               //Short = 3,
               //UShort = 4,
               Int = 5,
               UInt = 6,
               Long = 10,
               ULong = 20,
               //Decimal = new decimal(2.45),
               //Float = 128,
               //Double = 3.6,
            };
            
            var data = EventExtension.FireEvent(eventDemo);
            data.Address.ShouldBeNull();
            data.Topics.Count.ShouldBeGreaterThanOrEqualTo(6);
        }
        
        [Fact]
        public void Pack_Test()
        {
            //var value = (byte)32;
            //var value = (sbyte)32;
            //var value = 'c';
            //var value = (short) 5;
            //var value = (ushort) 5;
            //var value = (decimal) 3.45;
            //var value = (float)124;
            var value = (double)36.4;
            Should.Throw<System.Exception>(()=>ParamsPacker.Pack(value));
        }
    }
    
    public static class EventExtension
    {
        public static LogEvent FireEvent<TEvent>(TEvent e) where TEvent : Event
        {
            var data = EventParser<TEvent>.ToLogEvent(e);
            return data;
        }
    }
    
    public class EventDemo : Event {
        //Index
        [Indexed] public Address IndexAddress { get; set; }
        [Indexed] public string IndexSymbol { get; set; }
        //[Indexed] public byte IndexByte { get; set; }
        //[Indexed] public sbyte IndexSbyte { get; set; }
        //[Indexed] public char IndexChar { get; set; }
        //[Indexed] public short IndexShort { get; set; }
        //[Indexed] public ushort IndexUShort { get; set; }
        [Indexed] public int IndexInt { get; set; }
        [Indexed] public uint IndexUInt { get; set; }
        [Indexed] public long IndexLong { get; set; }        
        [Indexed] public ulong IndexULong { get; set; }
        //[Indexed] public decimal IndexDecimal { get; set; }
        //[Indexed] public float IndexFloat { get; set; }
        //[Indexed] public double IndexDouble { get; set; }
        
        //NonIndex
        public Address Address { get; set; }
        public string Symbol { get; set; }
        //public byte Byte { get; set; }
        //public sbyte Sbyte { get; set; }
        //public char Char { get; set; }
        //public short Short { get; set; }
        //public ushort UShort { get; set; }
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }        
        public ulong ULong { get; set; }
        //public decimal Decimal { get; set; }
        //public float Float { get; set; }
        //public double Double { get; set; }
    }
}