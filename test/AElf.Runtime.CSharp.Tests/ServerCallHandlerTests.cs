using System;
using AElf.CSharp.Core;
using AElf.Runtime.CSharp.Tests.TestContract;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp
{
    public class ServerCallHandlerTests : CSharpRuntimeTestBase
    {
        [Fact]
        public void Method_Type_Test()
        {
            var callHandler = CreateServerCallHandler();
            var isView = callHandler.IsView();
            isView.ShouldBeFalse();
            
            callHandler = CreateServerCallHandler(true);
            isView = callHandler.IsView();
            isView.ShouldBeTrue();
        }

        [Fact]
        public void Execute_Test()
        {
            var callHandler = CreateServerCallHandler();
            var input = new StringInput
            {
                StringValue = "test"
            };
            var inputArray = input.ToByteArray();
            var result = callHandler.Execute(inputArray);
            result.ShouldNotBeNull();

            var objectInfo = callHandler.ReturnBytesToObject(result);
            var outValue = (StringOutput) objectInfo;
            outValue.StringValue.ShouldBe("test");
        }

        [Fact]
        public void InputBytesToObject_Test()
        {
            var callHandler = CreateServerCallHandler();
            var input = new StringInput
            {
                StringValue = "test1"
            };
            var objInfo = callHandler.InputBytesToObject(input.ToByteArray());
            objInfo.ShouldNotBeNull();

            var input1 = (StringInput) objInfo;
            input1.ShouldBe(input);
        }

        [Fact]
        public void InputBytesToString_Test()
        {
            var callHandler = CreateServerCallHandler();
            var input = new StringInput
            {
                StringValue = "test1"
            };
            var objString = callHandler.InputBytesToString(input.ToByteArray());
            objString.ShouldBe("{ \"stringValue\": \"test1\" }");
        }

        [Fact]
        public void ReturnBytesToObject_Test()
        {
            var callHandler = CreateServerCallHandler();
            var output = new StringOutput
            {
                StringValue = "test-out"
            };
            var objInfo = callHandler.ReturnBytesToObject(output.ToByteArray());
            objInfo.ShouldNotBeNull();

            var output1 = (StringOutput) objInfo;
            output1.ShouldBe(output);
        }

        [Fact]
        public void ReturnBytesToString_Test()
        {
            var callHandler = CreateServerCallHandler();
            var output = new StringOutput()
            {
                StringValue = "test-out"
            };
            var objString = callHandler.ReturnBytesToString(output.ToByteArray());
            objString.ShouldBe("{ \"stringValue\": \"test-out\" }");
        }

        private IServerCallHandler CreateServerCallHandler(bool isView = false)
        {
            var method = CreateMethod(isView);
            var handler = CreateHandler();
            return ServerCalls.UnaryCall(method, handler);
        }

        private Method<StringInput, StringOutput> CreateMethod(bool isView)
        {
            Func<StringInput, byte[]> serializer = input => input.ToByteArray();
            Func<byte[], StringInput> deserializer = input => StringInput.Parser.ParseFrom(input);

            Func<StringOutput, byte[]> serializer1 = input => input.ToByteArray();
            Func<byte[], StringOutput> deserializer1 = input => StringOutput.Parser.ParseFrom(input);

            return new Method<StringInput, StringOutput>(isView ? MethodType.View : MethodType.Action,
                nameof(TestContract), nameof(TestContract.TestStringState),
                new Marshaller<StringInput>(serializer, deserializer),
                new Marshaller<StringOutput>(serializer1, deserializer1));
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