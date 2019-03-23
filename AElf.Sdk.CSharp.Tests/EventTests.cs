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
        [Fact]
        public void Parse_ReferenceData() 
        {
            var transactionEvent = new TransactionEvent 
            {
                IndexTransaction = new Transaction { From = Address.Generate(), To = Address.Generate(), MethodName = "Test1" },
                NonIndexTransaction = new Transaction { From = Address.Generate(), To = Address.Generate(), MethodName = "Test2" }
            };
            var transactionLog = EventExtension.FireEvent(transactionEvent);
            transactionLog.Topics.Count.ShouldBe(2);

            var addressEvent = new AddressEvent 
            {
                IndexAddress = Address.Generate(),
                NonIndexAddress = Address.Generate()
            };
            var addressLog = EventExtension.FireEvent(addressEvent);
            addressLog.Topics.Count.ShouldBe(2);
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
        }

        [Fact]
        public void Parse_NotSupport_EventType() 
        {
            var notsupportEvent = new NotSupportEvent 
            {
                IndexDouble = (double) 36,
                IndexFloat = (float) 2.45,

                NonIndexDouble = (double) 27,
                NonIndexFloat = (float) 3.67
            };
            Should.Throw<System.Exception>(() => EventExtension.FireEvent(notsupportEvent));
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