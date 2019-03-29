using System.Collections.Generic;
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
        [Fact]
        public void Parse_ReferenceData() 
        {
            var transactionEvent = new TransactionEvent 
            {
                IndexTransaction = new Transaction { From = Address.Generate(), To = Address.Generate(), MethodName = "Test1" },
                NonIndexTransaction = new Transaction { From = Address.Generate(), To = Address.Generate(), MethodName = "Test2" }
            };
            var transactionLog = EventExtension.FireEvent(transactionEvent);
            transactionLog.Indexed.Count.ShouldBe(2);
            transactionLog.NonIndexed.ShouldBe(ByteString.CopyFrom(ParamsPacker.Pack(transactionEvent.NonIndexTransaction)));

            var addressEvent = new AddressEvent 
            {
                IndexAddress = Address.Generate(),
                NonIndexAddress = Address.Generate()
            };
            var addressLog = EventExtension.FireEvent(addressEvent);
            addressLog.Indexed.Count.ShouldBe(2);
            addressLog.NonIndexed.ShouldBe(ByteString.CopyFrom(ParamsPacker.Pack(addressEvent.NonIndexAddress)));
        }

        [Fact]
        public void Parse_StringEvent() 
        {
            var stringEvent = new StringEvent 
            {
                IndexString = "test1",
                NonIndexString = "test2"
            };
            var stringLog = EventExtension.FireEvent(stringEvent);
            stringLog.Indexed.Count.ShouldBe(2);
            stringLog.NonIndexed.ShouldBe(ByteString.CopyFrom(ParamsPacker.Pack(stringEvent.NonIndexString)));
        }

        [Fact]
        public void Parse_DataEvent() 
        {
            var dataEvent = new DataEvent 
            {
                IndexInt = (int) 8,
                IndexUInt = (uint) 12,
                IndexLong = (long) 24,
                IndexULong = (ulong) 32,

                NonIndexInt = (int) 36,
                NonIndexLong = (long) 48
            };
            var dataLog = EventExtension.FireEvent(dataEvent);
            dataLog.Indexed.Count.ShouldBe(5);
            dataLog.NonIndexed.ShouldBe(ByteString.CopyFrom(ParamsPacker.Pack(dataEvent.NonIndexInt, dataEvent.NonIndexLong)));
        }

        [Fact]
        public void Parse_MultiEvent() 
        {
            var multiEvent = new MultiEvent 
            {
                IndexTransaction = new Transaction { From = Address.Generate(), To = Address.Generate(), MethodName = "test_method" },
                IndexAddress = Address.Generate(),
                IndexString = "index test info",
                IndexInt = 24,
                IndexLong = 36,

                NonIndexAddress = Address.Generate(),
                NonIndexString = "non index test info",
                NonIndexInt = 48
            };
            var multiLog = EventExtension.FireEvent(multiEvent);
            multiLog.Indexed.Count.ShouldBe(6);
        }

        [Fact]
        public void Parse_NotSupport_ListIndexEvent()
        {
            var listIndexEvent = new ListIndexEvent
            {
                IndexAddressList = new List<Address>(){Address.Zero, Address.Genesis, Address.Generate()},
            };
            Should.Throw<System.Exception>(()=> EventExtension.FireEvent(listIndexEvent));
        }
        
        [Fact]
        public void Parse_NotSupport_ListNonIndexEvent()
        {
            var listEvent = new ListNonIndexEvent
            {
                AddressList = new List<Address>(){Address.Zero, Address.Genesis, Address.Generate()},
            };
            Should.Throw<System.Exception>(()=> EventExtension.FireEvent(listEvent));
        }

        [Fact]
        public void Parse_NotSupport_DoubleIndexEvent() 
        {
            var doubleIndexEvent = new NotSupportIndexDouble() 
            {
                IndexDouble = (double) 36.6
            };
            Should.Throw<System.Exception>(() => EventExtension.FireEvent(doubleIndexEvent));
        }
        
        [Fact]
        public void Parse_NotSupport_DoubleEvent() 
        {
            var doubleEvent = new NotSupportDouble 
            {
                NonIndexDouble = (double) 36.4
            };
            Should.Throw<System.Exception>(() => EventExtension.FireEvent(doubleEvent));
        }
        
        [Fact]
        public void Parse_NotSupport_FloatIndexEvent()
        {
            var floatIndexEvent = new NotSupportIndexFloat
            {
                IndexFloat = (float) 36.56
            };
            Should.Throw<System.Exception>(() => EventExtension.FireEvent(floatIndexEvent));
        }
        
        [Fact]
        public void Parse_NotSupport_FloatEvent() 
        {
            var floatEvent = new NotSupportFloat()
            {
                NonIndexFloat = (float) 32.84
            };
            Should.Throw<System.Exception>(() => EventExtension.FireEvent(floatEvent));
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
}