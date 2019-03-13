using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests
{
    public class ProtobufTests
    {
        [Fact]
        public void TestDescriptor()
        {
            var descriptorBytes = Hash.Descriptor.File.SerializedData;

            var file =  FileDescriptor.BuildFromByteStrings(new ByteString[] {descriptorBytes});

            var reg = TypeRegistry.FromFiles(file);

            var messageDescriptor = reg.Find("AElf.Common.Hash");
            
            //messageDescriptor.Name.ShouldBe("Hash");
        }
    }
}