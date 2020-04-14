using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Types.Tests
{
    public class ProtobufTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ProtobufTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void TestDescriptor()
        {
            var descriptorBytes = Hash.Descriptor.File.SerializedData;

            var file = FileDescriptor.BuildFromByteStrings(new ByteString[] {descriptorBytes});

            var reg = TypeRegistry.FromFiles(file);

            var messageDescriptor = reg.Find("aelf.Hash");

            messageDescriptor.Name.ShouldBe("Hash");

            var hash = HashHelper.ComputeFrom("hello");

            var json = JsonFormatter.Default.Format(hash);

            //JsonParser.Default.Parse(json, messageDescriptor);
            //it will not work, messageDescriptor clr type is null, the factory of parser is null

            var deserializedHash = (Hash) JsonParser.Default.Parse(json, Hash.Descriptor);
            deserializedHash.ShouldBe(hash);
        }
    }
}