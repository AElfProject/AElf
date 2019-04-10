using Shouldly;
using Xunit;

namespace AElf.Types.CSharp
{
    public class MarshallerTests : TypesCSharpTestBase
    {
        [Fact]
        public void StringMarshall_Test()
        {
            var stringMarshall = Marshallers.StringMarshaller;
            var stringValue = "test";
            var byteArray = stringMarshall.Serializer(stringValue);
            byteArray.ShouldNotBeNull();
            
            var result = stringMarshall.Deserializer(byteArray);
            result.ShouldBe(stringValue);
        }
    }
}