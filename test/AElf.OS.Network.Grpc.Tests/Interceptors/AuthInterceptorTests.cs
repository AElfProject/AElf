using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Shouldly;
using Xunit;
using Xunit.Sdk;

namespace AElf.OS.Network.Grpc
{
    public class AuthInterceptorTests : GrpcNetworkWithPeerTestBase
    {
        private readonly IPeerPool _peerPool;
        private readonly AuthInterceptor _authInterceptor;

        private Server _server;
        private Channel _channel;

        public AuthInterceptorTests()
        {
            _peerPool = GetRequiredService<IPeerPool>();
            _authInterceptor = GetRequiredService<AuthInterceptor>();
        }

        [Theory]
        [InlineData("Ping")]
        [InlineData("DoHandshake")]
        public async Task UnaryServerHandler_NoAuth_Test(string methodName)
        {
            var helper = new MockServiceBuilder();
            var unaryHandler = new UnaryServerMethod<string, string>((request, context) => Task.FromResult("ok"));
            var method = new Method<string, string>(
                MethodType.Unary,
                nameof(PeerService),
                methodName,
                Marshallers.StringMarshaller,
                Marshallers.StringMarshaller);
            var serverServiceDefinition = ServerServiceDefinition.CreateBuilder()
                .AddMethod(method, (request, context) => unaryHandler(request, context)).Build()
                .Intercept(_authInterceptor);
            helper.ServiceDefinition = serverServiceDefinition;
            _server = helper.GetServer();
            _server.Start();

            _channel = helper.GetChannel();

            var result = await Calls.AsyncUnaryCall(new CallInvocationDetails<string, string>(_channel, method, default),
                "");
            result.ShouldBe("ok");
        }

        [Fact]
        public async Task UnaryServerHandler_Auth_Failed()
        {
            var helper = new MockServiceBuilder();
            helper.UnaryHandler = new UnaryServerMethod<string, string>((request, context) => Task.FromResult("ok"));

            helper.ServiceDefinition = helper.ServiceDefinition.Intercept(_authInterceptor);
            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();
            
            await ShouldBeCancelRpcExceptionAsync(async () =>
                await Calls.AsyncUnaryCall(helper.CreateUnaryCall(), ""));

            var method = new Method<string, string>(MethodType.Unary, MockServiceBuilder.ServiceName, "Unary",
                Marshallers.StringMarshaller, Marshallers.StringMarshaller);

            var peer = _peerPool.GetPeersByHost("127.0.0.1").First();
            ((GrpcPeer) peer).InboundSessionId = new byte[] {1, 2, 3};
            var callInvoker = helper.GetChannel().Intercept(metadata =>
            {
                metadata = new Metadata
                {
                    { GrpcConstants.PubkeyMetadataKey, peer.Info.Pubkey}
                };
                return metadata;
            });

            await ShouldBeCancelRpcExceptionAsync(async () =>
                await callInvoker.AsyncUnaryCall(method, "localhost", new CallOptions(), ""));
            
            callInvoker = helper.GetChannel().Intercept(metadata =>
            {
                metadata =  new Metadata
                {
                    { GrpcConstants.PubkeyMetadataKey, peer.Info.Pubkey},
                    { GrpcConstants.SessionIdMetadataKey, new byte[] {4, 5, 6 }}
                };
                return metadata;
            });

            await ShouldBeCancelRpcExceptionAsync(async () =>
                await callInvoker.AsyncUnaryCall(method, "localhost", new CallOptions(), ""));

            ((GrpcPeer) peer).InboundSessionId = null;
            await ShouldBeCancelRpcExceptionAsync(async () =>
                await callInvoker.AsyncUnaryCall(method, "localhost", new CallOptions(), ""));
        }
        
        [Fact]
        public async Task UnaryServerHandler_Auth_Success()
        {
            var peer = _peerPool.GetPeersByHost("127.0.0.1").First();
            ((GrpcPeer) peer).InboundSessionId = new byte[] {1, 2, 3};
            
            var helper = new MockServiceBuilder();
            helper.UnaryHandler = new UnaryServerMethod<string, string>((request, context) =>
            {
                context.GetPeerInfo().ShouldBe(peer.ToString());
                return Task.FromResult("ok");
            });

            helper.ServiceDefinition = helper.ServiceDefinition.Intercept(_authInterceptor);
            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();

            var method = new Method<string, string>(MethodType.Unary, MockServiceBuilder.ServiceName, "Unary",
                Marshallers.StringMarshaller, Marshallers.StringMarshaller);

            var callInvoker = helper.GetChannel().Intercept(metadata =>
            {
                metadata =  new Metadata
                {
                    { GrpcConstants.PubkeyMetadataKey, peer.Info.Pubkey},
                    { GrpcConstants.SessionIdMetadataKey, new byte[] {1, 2, 3}}
                };
                return metadata;
            });

            var result = await callInvoker.AsyncUnaryCall(method, "localhost", new CallOptions(), "");
            result.ShouldBe("ok");
        }
        
        [Fact]
        public async Task ClientStreamingServerHandler_Auth_Failed()
        {
            var helper = new MockServiceBuilder();
            helper.ClientStreamingHandler = new ClientStreamingServerMethod<string, string>((request, context) => Task.FromResult("ok"));

            helper.ServiceDefinition = helper.ServiceDefinition.Intercept(_authInterceptor);
            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();
            
            await ShouldBeCancelRpcExceptionAsync(async () =>
                await Calls.AsyncClientStreamingCall(helper.CreateClientStreamingCall()).ResponseAsync);

            var method = new Method<string, string>(MethodType.ClientStreaming, MockServiceBuilder.ServiceName, "ClientStreaming",
                Marshallers.StringMarshaller, Marshallers.StringMarshaller);

            var peer = _peerPool.GetPeersByHost("127.0.0.1").First();
            ((GrpcPeer) peer).InboundSessionId = new byte[] {1, 2, 3};
            var callInvoker = helper.GetChannel().Intercept(metadata =>
            {
                metadata = new Metadata
                {
                    { GrpcConstants.PubkeyMetadataKey, peer.Info.Pubkey},
                    { GrpcConstants.SessionIdMetadataKey, new byte[] {4, 5, 6}}
                };
                return metadata;
            });

            await ShouldBeCancelRpcExceptionAsync(async () =>
                await callInvoker.AsyncClientStreamingCall(method, "localhost", new CallOptions()).ResponseAsync);
        }

        [Fact]
        public async Task ClientStreamingServerHandler_Auth_Success()
        {
            var peer = _peerPool.GetPeersByHost("127.0.0.1").First();
            ((GrpcPeer) peer).InboundSessionId = new byte[] {1, 2, 3};

            var helper = new MockServiceBuilder();
            helper.ClientStreamingHandler = new ClientStreamingServerMethod<string, string>((request, context) =>
            {
                context.GetPeerInfo().ShouldBe(peer.ToString());
                return Task.FromResult("ok");
            });

            helper.ServiceDefinition = helper.ServiceDefinition.Intercept(_authInterceptor);
            _server = helper.GetServer();
            _server.Start();
            _channel = helper.GetChannel();

            var method = new Method<string, string>(MethodType.ClientStreaming, MockServiceBuilder.ServiceName,
                "ClientStreaming",
                Marshallers.StringMarshaller, Marshallers.StringMarshaller);

            var callInvoker = helper.GetChannel().Intercept(metadata =>
            {
                metadata = new Metadata
                {
                    {GrpcConstants.PubkeyMetadataKey, peer.Info.Pubkey},
                    {GrpcConstants.SessionIdMetadataKey, new byte[] {1, 2, 3}}
                };
                return metadata;
            });

            var result = await callInvoker.AsyncClientStreamingCall(method, "localhost", new CallOptions()).ResponseAsync;
            result.ShouldBe("ok");
        }

        private async Task ShouldBeCancelRpcExceptionAsync(Func<Task> func)
        {
            try
            {
                await func();
                throw new XunitException("Should throw RpcException, but execute successfully.");
            }
            catch (RpcException e)
            {
                e.Status.StatusCode.ShouldBe(StatusCode.Cancelled);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _channel.ShutdownAsync().Wait();
            _server.ShutdownAsync().Wait();
        }
    }
}