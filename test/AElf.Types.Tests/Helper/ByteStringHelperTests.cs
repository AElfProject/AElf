using Xunit;
using Google.Protobuf;
using Shouldly;

namespace AElf.Types.Tests.Helper
{
    public class ByteStringHelperTests
    {
        [Fact]
        public void ByteString_Compare()
        {
            var byteString1 = ByteString.CopyFrom(new byte[] { });
            var byteString2 = ByteString.CopyFrom(new byte[1] { 02 });
            var byteString3 = ByteString.CopyFrom(new byte[2] { 04, 10 });
            var byteString4 = ByteString.CopyFrom(new byte[3] { 10, 12, 14 });
            var byteString5 = ByteString.CopyFrom(new byte[3] { 00, 12, 14 });

            ByteStringHelper.Compare(ByteString.Empty, ByteString.Empty).ShouldBe(0);
            ByteStringHelper.Compare(byteString1, byteString2).ShouldBe(0);
            ByteStringHelper.Compare(byteString2, byteString3).ShouldBe(-1);
            ByteStringHelper.Compare(byteString3, byteString4).ShouldBe(-1);
            ByteStringHelper.Compare(byteString4, byteString5).ShouldBe(1);
        }
    }
}