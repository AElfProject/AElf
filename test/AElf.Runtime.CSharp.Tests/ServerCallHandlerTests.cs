using System;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp
{
    public class ServerCallHandlerTests : CSharpRuntimeTestBase
    {
        private IServerCallHandler _callHandler;

        public ServerCallHandlerTests()
        {
            var method = CreateMethod();
            var handler = CreateHandler();
            _callHandler = ServerCalls.UnaryCall(method, handler);
        }

        [Fact]
        public void Method_Type_Test()
        {
            var isView = _callHandler.IsView();
            isView.ShouldBeFalse();
        }

        [Fact]
        public void Execute_Test()
        {
            var input = new StringInput
            {
                StringValue = "test"
            };
            var inputArray = input.ToByteArray();
            var result = _callHandler.Execute(inputArray);
            result.ShouldNotBeNull();

            var objectInfo = _callHandler.ReturnBytesToObject(result);
            var outValue = (StringOutput) objectInfo;
            outValue.StringValue.ShouldBe("test");
        }

        [Fact]
        public void InputBytesToObject_Test()
        {
            var input = new StringInput
            {
                StringValue = "test1"
            };
            var objInfo = _callHandler.InputBytesToObject(input.ToByteArray());
            objInfo.ShouldNotBeNull();
            
            var input1 = (StringInput) objInfo;
            input1.ShouldBe(input);
        }

        [Fact]
        public void InputBytesToString_Test()
        {
            var input = new StringInput
            {
                StringValue = "test1"
            };
            var objString = _callHandler.InputBytesToString(input.ToByteArray());
            objString.ShouldNotBeNullOrEmpty();
            objString.Contains("test1").ShouldBeTrue();
        }
        [Fact]
        public void ReturnBytesToObject_Test()
        {
            var output = new StringOutput
            {
                StringValue = "test-out"
            };
            var objInfo = _callHandler.ReturnBytesToObject(output.ToByteArray());
            objInfo.ShouldNotBeNull();

            var output1 = (StringOutput) objInfo;
            output1.ShouldBe(output);
        }

        [Fact]
        public void ReturnBytesToString_Test()
        {
            var output = new StringOutput()
            {
                StringValue = "test-out"
            };
            var objString = _callHandler.ReturnBytesToString(output.ToByteArray());
            objString.ShouldNotBeNullOrEmpty();
            objString.Contains("test-out").ShouldBeTrue();
        }

        private Method<StringInput, StringOutput> CreateMethod()
        {
            Func<StringInput, byte[]> serializer = input => input.ToByteArray();
            Func<byte[], StringInput> deserializer = input => StringInput.Parser.ParseFrom(input);
            
            Func<StringOutput, byte[]> serializer1 = input => input.ToByteArray();
            Func<byte[], StringOutput> deserializer1 = input => StringOutput.Parser.ParseFrom(input);
            
            return new Method<StringInput, StringOutput>(MethodType.Action, nameof(TestContract), nameof(TestContract.TestStringState),
                new Marshaller<StringInput>(serializer, deserializer), new Marshaller<StringOutput>(serializer1, deserializer1));
        }

        private UnaryServerMethod<StringInput, StringOutput> CreateHandler()
        {
            UnaryServerMethod<StringInput, StringOutput> handler = 
                input => new StringOutput
                {
                    StringValue = input.StringValue
                };
            return handler;
        }
    }
}