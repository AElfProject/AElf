using Shouldly;
using Xunit;

namespace AElf.CrossChain.Grpc
{
    public class UriHelperTests
    {
        [Fact]
        public void GrpcUrl_ParseTest()
        {
            //wrong format
            {
                string address = "127.0.0.1:8000";
                var parsed = GrpcUriHelper.TryParsePrefixedEndpoint(address, out var endpoint);

                parsed.ShouldBeFalse();
                endpoint.ShouldBeNull();
            }
            
            //correct format
            {
                string address = "ipv4:127.0.0.1:8000";
                var parsed = GrpcUriHelper.TryParsePrefixedEndpoint(address, out var endpoint);
                
                parsed.ShouldBeTrue();
                endpoint.ToString().ShouldBe("127.0.0.1:8000");
                endpoint.Port.ShouldBe(8000);
            }
        }
    }
}