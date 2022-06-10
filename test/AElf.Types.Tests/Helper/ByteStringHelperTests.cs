using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helper;

public class ByteStringHelperTests
{
    [Fact]
    public void ByteString_Compare_Test()
    {
        var byteString1 = ByteString.CopyFrom();
        var byteString2 = ByteString.CopyFrom(02);
        var byteString3 = ByteString.CopyFrom(04, 10);
        var byteString4 = ByteString.CopyFrom(10, 12, 14);
        var byteString5 = ByteString.CopyFrom(00, 12, 14);
        var byteString6 = ByteString.CopyFrom(00, 12, 14);

        ByteStringHelper.Compare(ByteString.Empty, ByteString.Empty).ShouldBe(0);
        ByteStringHelper.Compare(byteString1, byteString2).ShouldBe(0);
        ByteStringHelper.Compare(byteString2, byteString3).ShouldBe(-1);
        ByteStringHelper.Compare(byteString3, byteString4).ShouldBe(-1);
        ByteStringHelper.Compare(byteString4, byteString5).ShouldBe(1);
        ByteStringHelper.Compare(byteString5, byteString6).ShouldBe(0);
    }

    [Fact]
    public void ByteString_FromHexString()
    {
        var hexString = HashHelper.ComputeFrom("hash").ToHex();
        var result = ByteStringHelper.FromHexString(hexString);
        result.ShouldNotBe(null);
    }
}