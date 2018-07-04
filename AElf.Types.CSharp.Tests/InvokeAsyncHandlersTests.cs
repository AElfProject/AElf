using System;
using System.Threading.Tasks;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using Xunit;

namespace AElf.Types.CSharp.Tests
{

    class InvokeAsyncHandlersTestClass
    {
        public async Task<bool> BoolReturnTypeMethod()
        {
            return await Task.FromResult(true);
        }

        public async Task<int> Int32ReturnTypeMethod()
        {
            return await Task.FromResult(-32);
        }

        public async Task<uint> UInt32ReturnTypeMethod()
        {
            return await Task.FromResult((uint)32);
        }

        public async Task<long> Int64ReturnTypeMethod()
        {
            return await Task.FromResult(-64);
        }

        public async Task<ulong> UInt64ReturnTypeMethod()
        {
            return await Task.FromResult((ulong)64);
        }

        public async Task<string> StringReturnTypeMethod()
        {
            return await Task.FromResult("AElf");
        }

        public async Task<byte[]> BytesReturnTypeMethod()
        {
            return await Task.FromResult(new byte[] { 0x1, 0x2, 0x3 });
        }
    }

    public class InvokeAsyncHandlersTests
    {
        private Func<object, object[], Task<IMessage>> GetHandler(System.Type returnType)
        {
            System.Type type = typeof(InvokeAsyncHandlersTestClass);
            if (returnType == typeof(bool))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForBoolReturnType(type.GetMethod("BoolReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(int))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForInt32ReturnType(type.GetMethod("Int32ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(uint))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForUInt32ReturnType(type.GetMethod("UInt32ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(long))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForInt64ReturnType(type.GetMethod("Int64ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(ulong))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForUInt64ReturnType(type.GetMethod("UInt64ReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(string))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForStringReturnType(type.GetMethod("StringReturnTypeMethod"), i, p);
            }
            if (returnType == typeof(byte[]))
            {
                return async (i, p) => await InvokeAsyncHandlers.ForBytesReturnType(type.GetMethod("BytesReturnTypeMethod"), i, p);
            }
            return null;
        }

        [Fact]
        public async Task Test()
        {
            InvokeAsyncHandlersTestClass obj = new InvokeAsyncHandlersTestClass();
            var parameters = new object[] { };
            Assert.Equal(new BoolValue() { Value = true }, await GetHandler(typeof(bool))(obj, parameters));
            Assert.Equal(new SInt32Value() { Value = -32 }, await GetHandler(typeof(int))(obj, parameters));
            Assert.Equal(new UInt32Value() { Value = 32 }, await GetHandler(typeof(uint))(obj, parameters));
            Assert.Equal(new SInt64Value() { Value = -64 }, await GetHandler(typeof(long))(obj, parameters));
            Assert.Equal(new UInt64Value() { Value = 64 }, await GetHandler(typeof(ulong))(obj, parameters));
            Assert.Equal(new StringValue() { Value = "AElf" }, await GetHandler(typeof(string))(obj, parameters));
            Assert.Equal(new BytesValue() { Value = ByteString.CopyFrom(new byte[] { 0x1, 0x2, 0x3 }) }, await GetHandler(typeof(byte[]))(obj, parameters));
        }
    }
}
