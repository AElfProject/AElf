using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Allows setting up a mock service in the client-server tests easily.
    /// </summary>
    public class MockServiceBuilder
    {
        public const string ServiceName = "tests.Test";

        private readonly string _host;
        private readonly IEnumerable<ChannelOption> _channelOptions;

        private readonly Method<string, string> _unaryMethod;
        private readonly Method<string, string> _clientStreamingMethod;
        private readonly Method<string, string> _serverStreamingMethod;
        private readonly Method<string, string> _duplexStreamingMethod;

        private UnaryServerMethod<string, string> _unaryHandler;
        private ClientStreamingServerMethod<string, string> _clientStreamingHandler;
        private ServerStreamingServerMethod<string, string> _serverStreamingHandler;
        private DuplexStreamingServerMethod<string, string> _duplexStreamingHandler;

        private Server _server;
        private Channel _channel;

        public MockServiceBuilder(string host = null, Marshaller<string> marshaller = null, IEnumerable<ChannelOption> channelOptions = null)
        {
            _host = host ?? "localhost";
            _channelOptions = channelOptions;
            marshaller ??= Marshallers.StringMarshaller;

            _unaryMethod = new Method<string, string>(
                MethodType.Unary,
                ServiceName,
                "Unary",
                marshaller,
                marshaller);

            _clientStreamingMethod = new Method<string, string>(
                MethodType.ClientStreaming,
                ServiceName,
                "ClientStreaming",
                marshaller,
                marshaller);

            _serverStreamingMethod = new Method<string, string>(
                MethodType.ServerStreaming,
                ServiceName,
                "ServerStreaming",
                marshaller,
                marshaller);

            _duplexStreamingMethod = new Method<string, string>(
                MethodType.DuplexStreaming,
                ServiceName,
                "DuplexStreaming",
                marshaller,
                marshaller);

            ServiceDefinition = ServerServiceDefinition.CreateBuilder()
                .AddMethod(_unaryMethod, (request, context) => _unaryHandler(request, context))
                .AddMethod(_clientStreamingMethod, (requestStream, context) => _clientStreamingHandler(requestStream, context))
                .AddMethod(_serverStreamingMethod, (request, responseStream, context) => _serverStreamingHandler(request, responseStream, context))
                .AddMethod(_duplexStreamingMethod, (requestStream, responseStream, context) => _duplexStreamingHandler(requestStream, responseStream, context))
                .Build();

            var defaultStatus = new Status(StatusCode.Unknown, "Default mock implementation. Please provide your own.");

            _unaryHandler = new UnaryServerMethod<string, string>((request, context) =>
            {
                context.Status = defaultStatus;
                return Task.FromResult("");
            });

            _clientStreamingHandler = new ClientStreamingServerMethod<string, string>((requestStream, context) =>
            {
                context.Status = defaultStatus;
                return Task.FromResult("");
            });

            _serverStreamingHandler = new ServerStreamingServerMethod<string, string>((request, responseStream, context) =>
            {
                context.Status = defaultStatus;
                return TaskUtils.CompletedTask;
            });

            _duplexStreamingHandler = new DuplexStreamingServerMethod<string, string>((requestStream, responseStream, context) =>
            {
                context.Status = defaultStatus;
                return TaskUtils.CompletedTask;
            });
        }

        /// <summary>
        /// Returns the default server for this service and creates one if not yet created.
        /// </summary>
        public Server GetServer()
        {
            if (_server == null)
            {
                // Disable SO_REUSEPORT to prevent https://github.com/grpc/grpc/issues/10755
                _server = new Server(new[] { new ChannelOption(ChannelOptions.SoReuseport, 0) })
                {
                    Services = { ServiceDefinition },
                    Ports = { { Host, ServerPort.PickUnused, ServerCredentials.Insecure } }
                };
            }
            return _server;
        }

        /// <summary>
        /// Returns the default channel for this service and creates one if not yet created.
        /// </summary>
        public Channel GetChannel()
        {
            if (_channel == null)
            {
                _channel = new Channel(Host, GetServer().Ports.Single().BoundPort, ChannelCredentials.Insecure, _channelOptions);
            }
            return _channel;
        }

        public CallInvocationDetails<string, string> CreateUnaryCall(CallOptions options = default(CallOptions))
        {
            return new CallInvocationDetails<string, string>(_channel, _unaryMethod, options);
        }

        public CallInvocationDetails<string, string> CreateClientStreamingCall(CallOptions options = default(CallOptions))
        {
            return new CallInvocationDetails<string, string>(_channel, _clientStreamingMethod, options);
        }

        public CallInvocationDetails<string, string> CreateServerStreamingCall(CallOptions options = default(CallOptions))
        {
            return new CallInvocationDetails<string, string>(_channel, _serverStreamingMethod, options);
        }

        public CallInvocationDetails<string, string> CreateDuplexStreamingCall(CallOptions options = default(CallOptions))
        {
            return new CallInvocationDetails<string, string>(_channel, _duplexStreamingMethod, options);
        }

        public string Host => _host;
        public ServerServiceDefinition ServiceDefinition { get; set; }
      
        public UnaryServerMethod<string, string> UnaryHandler
        {
            get { return this._unaryHandler; }
            set { _unaryHandler = value; }
        }

        public ClientStreamingServerMethod<string, string> ClientStreamingHandler
        {
            get { return this._clientStreamingHandler; }
            set { _clientStreamingHandler = value; }
        }

        public ServerStreamingServerMethod<string, string> ServerStreamingHandler
        {
            get { return this._serverStreamingHandler; }
            set { _serverStreamingHandler = value; }
        }

        public DuplexStreamingServerMethod<string, string> DuplexStreamingHandler
        {
            get { return this._duplexStreamingHandler; }
            set { _duplexStreamingHandler = value; }
        }
    }
}