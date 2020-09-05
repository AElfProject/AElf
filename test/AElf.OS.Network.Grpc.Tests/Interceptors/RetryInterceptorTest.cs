using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptorTest : GrpcNetworkWithPeerTestBase
    {
        private Server _server;
        private Channel _channel;
        
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

            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();
            
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

            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();
            
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
            
            callCount = 0;

            await Assert.ThrowsAsync<AggregateException>(async () => await callInvoker.AsyncUnaryCall(new Method<string, string>(MethodType.Unary, 
                    MockServiceBuilder.ServiceName, "Unary", Marshallers.StringMarshaller, Marshallers.StringMarshaller), 
                "localhost", new CallOptions(), ""));
            
            Assert.Equal(2, callCount);
        }
        
        [Fact]
        public async Task Retry_Timeout_Test()
        {
            var helper = new MockServiceBuilder("localhost");
            int callCount = 0;
            helper.UnaryHandler = new UnaryServerMethod<string, string>((request, context) =>
            {
                callCount++;

                Task.Delay(1000).Wait();
                
                return Task.FromResult("ok");
            });

            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();
            
            var callInvoker = helper.GetChannel().Intercept(new RetryInterceptor());
            
            var metadata = new Metadata {{ GrpcConstants.RetryCountMetadataKey, "1"}};

            var exception = await Assert.ThrowsAsync<AggregateException>(async () => await callInvoker.AsyncUnaryCall(
                new Method<string, string>(MethodType.Unary,
                    MockServiceBuilder.ServiceName, "Unary", Marshallers.StringMarshaller,
                    Marshallers.StringMarshaller),
                "localhost", new CallOptions().WithHeaders(metadata), ""));

            var rpcException = exception.InnerExceptions[0] as RpcException;
            rpcException.ShouldNotBeNull();
            rpcException.StatusCode.ShouldBe(StatusCode.DeadlineExceeded);

            Assert.Equal(2, callCount);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _channel.ShutdownAsync().Wait();
            _server.ShutdownAsync().Wait();
        }
    }
}