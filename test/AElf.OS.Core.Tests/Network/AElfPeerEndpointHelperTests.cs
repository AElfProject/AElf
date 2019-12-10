using AElf.OS.Network.Helpers;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class AElfPeerEndpointHelperTests : OSCoreNetworkServiceTestBase
    {
        [Theory]
        [InlineData("aelf.bp1.cn:8907", true, 8907)]
        [InlineData("aelf.bp1.cn", true,  NetworkConstants.DefaultPeerPort)]
        [InlineData("127.0.0.1:13000", true, 13000)]
        [InlineData("0.0.0.0:100", true, 100)]
        [InlineData("0.0.0.0", true, NetworkConstants.DefaultPeerPort)]
        [InlineData("::1", true, NetworkConstants.DefaultPeerPort)]
        [InlineData("::", true, NetworkConstants.DefaultPeerPort)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("aelf.bp1.cn:8907333", false)]
        [InlineData("aelf.bp1.cn:abcs", false)]
        public void ParsingTest(string endpointToParse, bool isValid, int expectedPort = 0)
        {
            AElfPeerEndpointHelper.TryParse(endpointToParse, out var endpoint).ShouldBe(isValid);
            
            if (isValid)
                endpoint.Port.ShouldBe(expectedPort);
        }
    }
}