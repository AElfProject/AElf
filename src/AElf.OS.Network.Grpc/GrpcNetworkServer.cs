using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using GuerrillaNtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Implements and manages the lifecycle of the network layer.
/// </summary>
public class GrpcNetworkServer : IAElfNetworkServer, ISingletonDependency
{
    private readonly AuthInterceptor _authInterceptor;
    private readonly IConnectionService _connectionService;

    private readonly PeerService.PeerServiceBase _serverService;

    private Server _server;

    public GrpcNetworkServer(PeerService.PeerServiceBase serverService, IConnectionService connectionService,
        AuthInterceptor authInterceptor)
    {
        _serverService = serverService;
        _connectionService = connectionService;
        _authInterceptor = authInterceptor;

        Logger = NullLogger<GrpcNetworkServer>.Instance;
        EventBus = NullLocalEventBus.Instance;
    }

    private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
    public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

    public ILocalEventBus EventBus { get; set; }
    public ILogger<GrpcNetworkServer> Logger { get; set; }

    public async Task StartAsync()
    {
        await StartListeningAsync();
        await DialBootNodesAsync();

        await EventBus.PublishAsync(new NetworkInitializedEvent());
    }

    /// <summary>
    ///     Connects to a node with the given ip address and adds it to the node's peer pool.
    /// </summary>
    /// <param name="endpoint">the ip address of the distant node</param>
    /// <returns>True if the connection was successful, false otherwise</returns>
    public async Task<bool> ConnectAsync(DnsEndPoint endpoint)
    {
        return await _connectionService.ConnectAsync(endpoint);
    }

    public async Task DisconnectAsync(IPeer peer, bool sendDisconnect = false)
    {
        await _connectionService.DisconnectAsync(peer, sendDisconnect);
    }

    public async Task<bool> TrySchedulePeerReconnectionAsync(IPeer peer)
    {
        return await _connectionService.TrySchedulePeerReconnectionAsync(peer);
    }

    public void CheckNtpDrift()
    {
        TimeSpan offset;
        using (var ntp = new NtpClient(Dns.GetHostAddresses("pool.ntp.org")[0]))
        {
            offset = ntp.GetCorrectionOffset();
        }

        if (offset.Duration().TotalMilliseconds > NetworkConstants.DefaultNtpDriftThreshold)
            Logger.LogWarning($"NTP clock drift is more that {NetworkConstants.DefaultNtpDriftThreshold} ms : " +
                              $"{offset.Duration().TotalMilliseconds} ms");
    }

    public async Task<bool> CheckEndpointAvailableAsync(DnsEndPoint endpoint)
    {
        return await _connectionService.CheckEndpointAvailableAsync(endpoint);
    }

    public async Task StopAsync(bool gracefulDisconnect = true)
    {
        try
        {
            await _server.ShutdownAsync();
        }
        catch (InvalidOperationException)
        {
            // if server already shutdown, we continue and clear the channels.
        }

        await _connectionService.DisconnectPeersAsync(gracefulDisconnect);
    }

    /// <summary>
    ///     Starts gRPC's server by binding the peer services, sets options and adds interceptors.
    /// </summary>
    internal Task StartListeningAsync()
    {
        ServerServiceDefinition serviceDefinition = PeerService.BindService(_serverService);

        if (_authInterceptor != null)
            serviceDefinition = serviceDefinition.Intercept(_authInterceptor);

        var serverOptions = new List<ChannelOption>
        {
            new(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
            new(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength),
            new(GrpcConstants.GrpcArgKeepalivePermitWithoutCalls, GrpcConstants.GrpcArgKeepalivePermitWithoutCallsOpen),
            new(GrpcConstants.GrpcArgHttp2MaxPingsWithoutData, GrpcConstants.GrpcArgHttp2MaxPingsWithoutDataVal),
            new(GrpcConstants.GrpcArgKeepaliveTimeoutMs, GrpcConstants.GrpcArgKeepaliveTimeoutMsVal),
            new(GrpcConstants.GrpcArgKeepaliveTimeMs, GrpcConstants.GrpcArgKeepaliveTimeMsVal)
        };

        // setup service
        _server = new Server(serverOptions);
        _server.Services.Add(serviceDefinition);

        var serverCredentials = CreateCredentials();

        // setup encrypted endpoint	
        _server.Ports.Add(new ServerPort(IPAddress.Any.ToString(), NetworkOptions.ListeningPort, serverCredentials));

        return Task.Run(() =>
        {
            _server.Start();

            foreach (var port in _server.Ports)
                Logger.LogDebug($"Server listening on {port.Host}:{port.BoundPort}.");
        });
    }

    private SslServerCredentials CreateCredentials()
    {
        var keyCertificatePair = TlsHelper.GenerateKeyCertificatePair();
        return new SslServerCredentials(new List<KeyCertificatePair> { keyCertificatePair });
    }

    /// <summary>
    ///     Connects to the boot nodes provided in the network options.
    /// </summary>
    private async Task DialBootNodesAsync()
    {
        if (NetworkOptions.BootNodes == null || !NetworkOptions.BootNodes.Any())
        {
            Logger.LogWarning("Boot nodes list is empty.");
            return;
        }

        var taskList = NetworkOptions.BootNodes
            .Select(async node =>
            {
                var dialed = false;

                if (!AElfPeerEndpointHelper.TryParse(node, out var endpoint))
                {
                    Logger.LogWarning($"Could not parse endpoint {node}.");
                    return;
                }

                try
                {
                    dialed = await _connectionService.ConnectAsync(endpoint);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, $"Connect peer failed {node}.");
                }

                if (!dialed)
                    await _connectionService.SchedulePeerReconnection(endpoint);
            }).ToList();

        await Task.WhenAll(taskList.ToArray<Task>());
    }

    public async Task<bool> BuildStreamForPeerAsync(IPeer peer)
    {
        return await _connectionService.BuildStreamForPeerAsync(peer);
    }
}