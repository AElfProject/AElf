using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptorTest : GrpcNetworkWithPeerTestBase
    {
        [Fact]
        public async Task RetryDoesNotExceedSuccess()
        {
            var helper = new MockServiceBuilder("localhost");
            int callCount = 0;
            helper.UnaryHandler = new UnaryServerMethod<string, string>((request, context) =>
            {
                callCount++;

                if (callCount == 1)
                    context.Status = new Status(StatusCode.Cancelled, "");
                
                return Task.FromResult("ok");
            });

            var server = helper.GetServer();
            server.Start();
            
            var callInvoker = helper.GetChannel().Intercept(new RetryInterceptor());
            
            var metadata = new Metadata {{ GrpcConstants.RetryCountMetadataKey, "5"}};
            
            await callInvoker.AsyncUnaryCall(new Method<string, string>(MethodType.Unary, 
                    MockServiceBuilder.ServiceName, "Unary", Marshallers.StringMarshaller, Marshallers.StringMarshaller), 
                "localhost", new CallOptions().WithHeaders(metadata), "");
            
            Assert.Equal(2, callCount);
        }
        
        [Fact]
        public async Task RetryHeaderDecidesRetryCount()
        {
            var helper = new MockServiceBuilder("localhost");
            int callCount = 0;
            helper.UnaryHandler = new UnaryServerMethod<string, string>((request, context) =>
            {
                callCount++;
                context.Status = new Status(StatusCode.Cancelled, "");
                return Task.FromResult("ok");
            });

            var server = helper.GetServer();
            server.Start();
            
            var callInvoker = helper.GetChannel().Intercept(new RetryInterceptor());
            
            var metadata = new Metadata {{ GrpcConstants.RetryCountMetadataKey, "0"}};
            
            await Assert.ThrowsAsync<AggregateException>(async () => await callInvoker.AsyncUnaryCall(new Method<string, string>(MethodType.Unary, 
                        MockServiceBuilder.ServiceName, "Unary", Marshallers.StringMarshaller, Marshallers.StringMarshaller), 
                    "localhost", new CallOptions().WithHeaders(metadata), ""));
            
            Assert.Equal(1, callCount);

            callCount = 0;
            var oneRetryMetadata = new Metadata {{ GrpcConstants.RetryCountMetadataKey, "1"}};

            await Assert.ThrowsAsync<AggregateException>(async () => await callInvoker.AsyncUnaryCall(new Method<string, string>(MethodType.Unary, 
                    MockServiceBuilder.ServiceName, "Unary", Marshallers.StringMarshaller, Marshallers.StringMarshaller), 
                "localhost", new CallOptions().WithHeaders(oneRetryMetadata), ""));
            
            Assert.Equal(2, callCount);
            
            await server.ShutdownAsync();
        }
    }
}