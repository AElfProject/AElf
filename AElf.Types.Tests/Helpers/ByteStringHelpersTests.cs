using AElf.Common;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helpers
{
    public class ByteStringHelpersTests
    {
        [Fact]
        public void ByteString_Compare()
        {
            var byteString1 = ByteString.CopyFrom();
            var byteString2 = ByteString.CopyFrom(02);
            var byteString3 = ByteString.CopyFrom(04, 10);
            var byteString4 = ByteString.CopyFrom(10, 12, 14);
            var byteString5 = ByteString.CopyFrom(00, 12, 14);

            ByteStringHelpers.Compare(ByteString.Empty, ByteString.Empty).ShouldBe(0);
            ByteStringHelpers.Compare(byteString1, byteString2).ShouldBe(0);
            ByteStringHelpers.Compare(byteString2, byteString3).ShouldBe(-1);
            ByteStringHelpers.Compare(byteString3, byteString4).ShouldBe(-1);
            ByteStringHelpers.Compare(byteString4, byteString5).ShouldBe(1);
        }
    }
}