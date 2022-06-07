using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Allows setting up a mock service in the client-server tests easily.
/// </summary>
public class MockServiceBuilder
{
    public const string ServiceName = "tests.Test";
    private readonly IEnumerable<ChannelOption> _channelOptions;
    private readonly Method<string, string> _clientStreamingMethod;
    private readonly Method<string, string> _duplexStreamingMethod;

    private readonly Method<string, string> _serverStreamingMethod;

    private readonly Method<string, string> _unaryMethod;
    private Channel _channel;

    private Server _server;

    public MockServiceBuilder(string host = null, Marshaller<string> marshaller = null,
        IEnumerable<ChannelOption> channelOptions = null)
    {
        Host = host ?? "localhost";
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
            .AddMethod(_unaryMethod, (request, context) => UnaryHandler(request, context))
            .AddMethod(_clientStreamingMethod,
                (requestStream, context) => ClientStreamingHandler(requestStream, context))
            .AddMethod(_serverStreamingMethod,
                (request, responseStream, context) => ServerStreamingHandler(request, responseStream, context))
            .AddMethod(_duplexStreamingMethod,
                (requestStream, responseStream, context) =>
                    DuplexStreamingHandler(requestStream, responseStream, context))
            .Build();

        var defaultStatus = new Status(StatusCode.Unknown, "Default mock implementation. Please provide your own.");

        UnaryHandler = (request, context) =>
        {
            context.Status = defaultStatus;
            return Task.FromResult("");
        };

        ClientStreamingHandler = (requestStream, context) =>
        {
            context.Status = defaultStatus;
            return Task.FromResult("");
        };

        ServerStreamingHandler = (request, responseStream, context) =>
        {
            context.Status = defaultStatus;
            return TaskUtils.CompletedTask;
        };

        DuplexStreamingHandler = (requestStream, responseStream, context) =>
        {
            context.Status = defaultStatus;
            return TaskUtils.CompletedTask;
        };
    }

    public string Host { get; }

    public ServerServiceDefinition ServiceDefinition { get; set; }

    public UnaryServerMethod<string, string> UnaryHandler { get; set; }

    public ClientStreamingServerMethod<string, string> ClientStreamingHandler { get; set; }

    public ServerStreamingServerMethod<string, string> ServerStreamingHandler { get; set; }

    public DuplexStreamingServerMethod<string, string> DuplexStreamingHandler { get; set; }

    /// <summary>
    ///     Returns the default server for this service and creates one if not yet created.
    /// </summary>
    public Server GetServer()
    {
        if (_server == null)
            // Disable SO_REUSEPORT to prevent https://github.com/grpc/grpc/issues/10755
            _server = new Server(new[] { new ChannelOption(ChannelOptions.SoReuseport, 0) })
            {
                Services = { ServiceDefinition },
                Ports = { { Host, ServerPort.PickUnused, ServerCredentials.Insecure } }
            };
        return _server;
    }

    /// <summary>
    ///     Returns the default channel for this service and creates one if not yet created.
    /// </summary>
    public Channel GetChannel()
    {
        if (_channel == null)
            _channel = new Channel(Host, GetServer().Ports.Single().BoundPort, ChannelCredentials.Insecure,
                _channelOptions);
        return _channel;
    }

    public CallInvocationDetails<string, string> CreateUnaryCall(CallOptions options = default)
    {
        return new CallInvocationDetails<string, string>(_channel, _unaryMethod, options);
    }

    public CallInvocationDetails<string, string> CreateClientStreamingCall(CallOptions options = default)
    {
        return new CallInvocationDetails<string, string>(_channel, _clientStreamingMethod, options);
    }

    public CallInvocationDetails<string, string> CreateServerStreamingCall(CallOptions options = default)
    {
        return new CallInvocationDetails<string, string>(_channel, _serverStreamingMethod, options);
    }

    public CallInvocationDetails<string, string> CreateDuplexStreamingCall(CallOptions options = default)
    {
        return new CallInvocationDetails<string, string>(_channel, _duplexStreamingMethod, options);
    }
}