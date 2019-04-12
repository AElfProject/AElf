using System;
using AElf.Runtime.CSharp.Tests.TestContract;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp
{
    public class MethodTests : TypesCSharpTestBase
    {
        [Fact]
        public void Method_Test()
        {
            var method = CreateMethod();
            
            method.ServiceName.ShouldBe(nameof(TestContract));
            method.Type.ShouldBe(MethodType.Action);
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
    }
}