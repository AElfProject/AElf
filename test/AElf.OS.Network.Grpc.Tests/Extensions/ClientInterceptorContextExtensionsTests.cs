using Grpc.Core;
using Grpc.Core.Interceptors;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc.Extensions
{
    public class ClientInterceptorContextExtensionsTests : GrpcNetworkTestBase
    {
        [Fact]
        public void GetHeader_Test()
        {
            var context = new ClientInterceptorContext<string, string>(null, null, new CallOptions());
            context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey).ShouldBeNull();
            
            context = new ClientInterceptorContext<string, string>(null, null, new CallOptions(new Metadata()));
            context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey).ShouldBeNull();
            
            Metadata data = new Metadata
            {
                { GrpcConstants.TimeoutMetadataKey, "TimeoutMetadata" },
                { GrpcConstants.RetryCountMetadataKey, "2" }
            }; 
            context = new ClientInterceptorContext<string, string>(null, null, new CallOptions(data));
            context.GetHeaderStringValue(GrpcConstants.TimeoutMetadataKey).ShouldBe("TimeoutMetadata");
            context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey).ShouldBe("2");
            
            context.GetHeaderStringValue(GrpcConstants.TimeoutMetadataKey, true).ShouldBe("TimeoutMetadata");
            context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey, true).ShouldBe("2");
            
            context.GetHeaderStringValue(GrpcConstants.TimeoutMetadataKey).ShouldBeNull();
            context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey).ShouldBeNull();
        }
    }
}