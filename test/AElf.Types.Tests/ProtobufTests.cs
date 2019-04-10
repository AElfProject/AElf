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

            var messageDescriptor = reg.Find("Hash");
            
            messageDescriptor.Name.ShouldBe("Hash");
            
            var hash=Hash.FromString("hello");

            var json = JsonFormatter.Default.Format(hash);

            //JsonParser.Default.Parse(json, messageDescriptor);
            //it will not work, messageDescriptor clr type is null, the factory of parser is null

            var deserializedHash = (Hash) JsonParser.Default.Parse(json, Hash.Descriptor);
            JsonParser.Default.Parse<Hash>(json);

        }
    }
}