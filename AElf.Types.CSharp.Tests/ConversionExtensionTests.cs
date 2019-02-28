using Google.Protobuf;
using Xunit;

namespace AElf.Types.CSharp
{
    public class ConversionExtensionTests
    {
        [Fact]
        public void ByteString_Convertion()
        {
            var bytes = new byte[]
            {
                125, 33, 27, 37, 202, 102, 171, 207, 118, 196, 214, 99, 224, 148, 157, 25, 230, 96, 125, 28, 227, 78, 1, 228, 24, 161, 56, 125, 186, 214
            };
            var byteString = ByteString.CopyFrom(bytes);
            var newBytes = byteString.DeserializeToBytes();
            Assert.Equal(newBytes, bytes);
        }
    }
}