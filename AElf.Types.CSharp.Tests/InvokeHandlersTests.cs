using System;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    class InvokeHandlersTestClass
    {
        public bool BoolReturnTypeMethod()
        {
            return true;
        }

        public int Int32ReturnTypeMethod()
        {
            return -32;
        }

        public uint UInt32ReturnTypeMethod()
        {
            return 32;
        }

        public long Int64ReturnTypeMethod()
        {
            return -64;
        }

        public ulong UInt64ReturnTypeMethod()
        {
            return 64;
        }

        public string StringReturnTypeMethod()
        {
            return "AElf";
        }

        public byte[] BytesReturnTypeMethod()
        {
            return new byte[] { 0x1, 0x2, 0x3 };
        }
    }

    public class InvokeHandlersTests
    {
        private Func<object, object[], IMessage> GetHandler(System.Type returnType)
        {
            System.Type type = typeof(InvokeHandlersTestClass);
            if (returnType == typeof(bool))
            {
                return (i, p) => InvokeHandlers.ForBoolReturnType(type.GetMethod("BoolReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(int))
            {
                return (i, p) => InvokeHandlers.ForInt32ReturnType(type.GetMethod("Int32ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(uint))
            {
                return (i, p) => InvokeHandlers.ForUInt32ReturnType(type.GetMethod("UInt32ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(long))
            {
                return (i, p) => InvokeHandlers.ForInt64ReturnType(type.GetMethod("Int64ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(ulong))
            {
                return (i, p) => InvokeHandlers.ForUInt64ReturnType(type.GetMethod("UInt64ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(string))
            {
                return (i, p) => InvokeHandlers.ForStringReturnType(type.GetMethod("StringReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(byte[]))
            {
                return (i, p) => InvokeHandlers.ForBytesReturnType(type.GetMethod("BytesReturnTypeMethod"), i, p);
            }
            return null;
        }

        [Fact]
        public void Test()
        {
            InvokeHandlersTestClass obj = new InvokeHandlersTestClass();
            var parameters = new object[] { };
            Assert.Equal(new BoolValue() { Value = true }, GetHandler(typeof(bool))(obj, parameters));
            Assert.Equal(new SInt32Value() { Value = -32 }, GetHandler(typeof(int))(obj, parameters));
            Assert.Equal(new UInt32Value() { Value = 32 }, GetHandler(typeof(uint))(obj, parameters));
            Assert.Equal(new SInt64Value() { Value = -64 }, GetHandler(typeof(long))(obj, parameters));
            Assert.Equal(new UInt64Value() { Value = 64 }, GetHandler(typeof(ulong))(obj, parameters));
            Assert.Equal(new StringValue() { Value = "AElf" }, GetHandler(typeof(string))(obj, parameters));
            Assert.Equal(new BytesValue() { Value = ByteString.CopyFrom(new byte[] { 0x1, 0x2, 0x3 }) }, GetHandler(typeof(byte[]))(obj, parameters));
        }
    }
}
