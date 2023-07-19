using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.X509;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc;

/// <summary>
///     Provides functionality to setup a connection to a distant node by exchanging some
///     low level information.
/// </summary>
public class PeerDialer : IPeerDialer
{
    private readonly IAccountService _accountService;
    private readonly IHandshakeProvider _handshakeProvider;
    private readonly IStreamTaskResourcePool _streamTaskResourcePool;
    public ILocalEventBus EventBus { get; set; }

    public PeerDialer(IAccountService accountService,
        IHandshakeProvider handshakeProvider, IStreamTaskResourcePool streamTaskResourcePool)
    {
        _accountService = accountService;
        _handshakeProvider = handshakeProvider;
        _streamTaskResourcePool = streamTaskResourcePool;
        EventBus = NullLocalEventBus.Instance;

        Logger = NullLogger<PeerDialer>.Instance;
    }

    private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
    public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

    public ILogger<PeerDialer> Logger { get; set; }

    /// <summary>
    ///     Given an IP address, will create a handshake to the distant node for
    ///     further communications.
    /// </summary>
    /// <returns>The created peer</returns>
    public async Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndpoint)
    {
        var client = await CreateClientAsync(remoteEndpoint);

        if (client == null)
            return null;

        var handshake = await _handshakeProvider.GetHandshakeAsync();
        var handshakeReply = await CallDoHandshakeAsync(client, remoteEndpoint, handshake);

        if (!await ProcessHandshakeReplyAsync(handshakeReply, remoteEndpoint))
        {
            await client.Channel.ShutdownAsync();
            return null;
        }

        var connectionInfo = new PeerConnectionInfo
        {
            Pubkey = handshakeReply.Handshake.HandshakeData.Pubkey.ToHex(),
            ConnectionTime = TimestampHelper.GetUtcNow(),
            ProtocolVersion = handshakeReply.Handshake.HandshakeData.Version,
            SessionId = handshakeReply.Handshake.SessionId.ToByteArray(),
            IsInbound = false,
            NodeVersion = handshakeReply.Handshake.HandshakeData.NodeVersion
        };
        GrpcPeer peer;

        if (UpgradeToStream(handshake, handshakeReply.Handshake))
        {
            peer = await DailStreamPeerAsync(client, remoteEndpoint, connectionInfo);
            if (peer == null) return peer;
        }
        else
        {
            peer = new GrpcPeer(client, remoteEndpoint, connectionInfo);
            peer.InboundSessionId = handshake.SessionId.ToByteArray();
        }


        Logger.LogDebug("peer sessionId {InboundSessionId} {sessionId}", peer.InboundSessionId.ToHex(), connectionInfo.SessionId.ToHex());
        peer.UpdateLastReceivedHandshake(handshakeReply.Handshake);
        peer.UpdateLastSentHandshake(handshake);
        return peer;
    }

    private async Task<bool> ProcessHandshakeReplyAsync(HandshakeReply handshakeReply, DnsEndPoint remoteEndpoint)
    {
        // verify handshake
        if (handshakeReply.Error != HandshakeError.HandshakeOk)
        {
            Logger.LogWarning("Handshake error: {remoteEndpoint} {Error}.", remoteEndpoint, handshakeReply.Error);

            return false;
        }

        if (await _handshakeProvider.ValidateHandshakeAsync(handshakeReply.Handshake) ==
            HandshakeValidationResult.Ok) return true;
        Logger.LogWarning("Connect error: {remoteEndpoint} {handshakeReply}.", remoteEndpoint, handshakeReply);
        return false;
    }


    public async Task<GrpcPeer> DialBackPeerByStreamAsync(DnsEndPoint remoteEndpoint, IAsyncStreamWriter<StreamMessage> responseStream, Handshake handshake)
    {
        Logger.LogWarning("receive stream ping reply");
        var info = new PeerConnectionInfo
        {
            Pubkey = handshake.HandshakeData.Pubkey.ToHex(),
            ConnectionTime = TimestampHelper.GetUtcNow(),
            SessionId = handshake.SessionId.ToByteArray(),
            ProtocolVersion = handshake.HandshakeData.Version,
            IsInbound = true,
            NodeVersion = handshake.HandshakeData.NodeVersion
        };
        var nodePubkey = (await _accountService.GetPublicKeyAsync()).ToHex();
        var meta = new Dictionary<string, string>()
        {
            { GrpcConstants.PubkeyMetadataKey, nodePubkey },
            { GrpcConstants.PeerInfoMetadataKey, info.ToString() }
        };
        Logger.LogWarning("DialBackPeerByStreamAsync meta={meta}", meta);
        var peer = new GrpcStreamBackPeer(remoteEndpoint, info, responseStream, _streamTaskResourcePool, meta);
        peer.SetStreamSendCallBack(async (ex, streamMessage, callTimes) =>
        {
            if (ex == null)
                Logger.LogDebug("streamRequest write success {times}-{requestId}-{messageType}-{this}-{latency}", callTimes, streamMessage.RequestId, streamMessage.MessageType, peer,
                    CommonHelper.GetRequestLatency(streamMessage.RequestId));
            else
            {
                Logger.LogError(ex, "streamRequest write fail, {requestId}-{messageType}-{this}", streamMessage.RequestId, streamMessage.MessageType, peer);
                await EventBus.PublishAsync(new StreamPeerExceptionEvent(ex, peer), false);
            }
        });
        peer.UpdateLastReceivedHandshake(handshake);

        return peer;
    }

    public async Task<bool> CheckEndpointAvailableAsync(DnsEndPoint remoteEndpoint)
    {
        var client = await CreateClientAsync(remoteEndpoint);

        if (client == null)
            return false;
        try
        {
            await PingNodeAsync(client, remoteEndpoint);
            await client.Channel.ShutdownAsync();
            return true;
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, $"Could not ping peer {remoteEndpoint}.");
            return false;
        }
    }

    public async Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint remoteEndpoint, Handshake handshake)
    {
        var client = await CreateClientAsync(remoteEndpoint);

        if (client == null)
            return null;

        await PingNodeAsync(client, remoteEndpoint);

        var peer = new GrpcPeer(client, remoteEndpoint, new PeerConnectionInfo
        {
            Pubkey = handshake.HandshakeData.Pubkey.ToHex(),
            ConnectionTime = TimestampHelper.GetUtcNow(),
            SessionId = handshake.SessionId.ToByteArray(),
            ProtocolVersion = handshake.HandshakeData.Version,
            IsInbound = true,
            NodeVersion = handshake.HandshakeData.NodeVersion
        });

        peer.UpdateLastReceivedHandshake(handshake);

        return peer;
    }

    /// <summary>
    ///     Calls the server side DoHandshake RPC method, in order to establish a 2-way connection.
    /// </summary>
    /// <returns>The reply from the server.</returns>
    private async Task<HandshakeReply> CallDoHandshakeAsync(GrpcClient client, DnsEndPoint remoteEndPoint,
        Handshake handshake)
    {
        HandshakeReply handshakeReply;

        try
        {
            var metadata = new Metadata
            {
                { GrpcConstants.RetryCountMetadataKey, "0" },
                { GrpcConstants.TimeoutMetadataKey, (NetworkOptions.PeerDialTimeout * 2).ToString() }
            };

            handshakeReply =
                await client.Client.DoHandshakeAsync(new HandshakeRequest { Handshake = handshake }, metadata);

            Logger.LogDebug($"Handshake to {remoteEndPoint} successful.");
        }
        catch (Exception)
        {
            await client.Channel.ShutdownAsync();
            throw;
        }

        return handshakeReply;
    }

    private bool UpgradeToStream(Handshake handshake, Handshake handshakeReply)
    {
        return handshake.HandshakeData.NodeVersion.GreaterThanSupportStreamMinVersion(NetworkOptions.SupportStreamMinVersion) &&
               handshakeReply.HandshakeData.NodeVersion.GreaterThanSupportStreamMinVersion(NetworkOptions.SupportStreamMinVersion);
    }

    private async Task<GrpcStreamPeer> DailStreamPeerAsync(GrpcClient client, DnsEndPoint remoteEndpoint, PeerConnectionInfo connectionInfo)
    {
        try
        {
            var nodePubkey = (await _accountService.GetPublicKeyAsync()).ToHex();
            var headers = new Metadata { new(GrpcConstants.GrpcRequestCompressKey, GrpcConstants.GrpcGzipConst) };
            var call = client.Client.RequestByStream(new CallOptions().WithHeaders(headers).WithDeadline(DateTime.MaxValue));
            var streamPeer = new GrpcStreamPeer(client, remoteEndpoint, connectionInfo, call, null, _streamTaskResourcePool,
                new Dictionary<string, string>()
                {
                    { GrpcConstants.PubkeyMetadataKey, nodePubkey },
                    { GrpcConstants.PeerInfoMetadataKey, connectionInfo.ToString() }
                });
            streamPeer.SetStreamSendCallBack(async (ex, streamMessage, callTimes) =>
            {
                if (ex == null)
                    Logger.LogDebug("streamRequest write success {times}-{requestId}-{messageType}-{this}-{latency}", callTimes, streamMessage.RequestId, streamMessage.MessageType, streamPeer,
                        CommonHelper.GetRequestLatency(streamMessage.RequestId));
                else
                {
                    Logger.LogError(ex, "streamRequest write fail, {requestId}-{messageType}-{this}", streamMessage.RequestId, streamMessage.MessageType, streamPeer);
                    await EventBus.PublishAsync(new StreamPeerExceptionEvent(ex, streamPeer), false);
                }
            });
            var success = await BuildStreamForPeerAsync(streamPeer, call);
            return success ? streamPeer : null;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "stream handle shake failed {remoteEndpoint}", remoteEndpoint);
            if (client.Channel.State == ChannelState.Idle || client.Channel.State == ChannelState.Ready)
                await client.Channel.ShutdownAsync();
            throw;
        }
    }

    public async Task<bool> BuildStreamForPeerAsync(GrpcStreamPeer streamPeer, AsyncDuplexStreamingCall<StreamMessage, StreamMessage> call = null)
    {
        call ??= streamPeer.BuildCall();
        if (call == null) return false;
        var tokenSource = new CancellationTokenSource();
        Task.Run(async () =>
        {
            try
            {
                await call.ResponseStream.ForEachAsync(async req =>
                {
                    Logger.LogDebug("listenReceive request={requestId} {streamType}-{messageType} latency={latency}", req.RequestId, req.StreamType, req.MessageType, CommonHelper.GetRequestLatency(req.RequestId));
                    await EventBus.PublishAsync(new StreamMessageReceivedEvent(req.ToByteString(), streamPeer.Info.Pubkey, req.RequestId), false);
                });
                Logger.LogWarning("listen end and complete {remoteEndPoint}", streamPeer.RemoteEndpoint.ToString());
            }
            catch (Exception e)
            {
                if (e is RpcException exception)
                    await EventBus.PublishAsync(new StreamPeerExceptionEvent(streamPeer.HandleRpcException(exception, "listen err {remoteEndPoint}"), streamPeer));
                Logger.LogError(e, "listen err {remoteEndPoint}", streamPeer.RemoteEndpoint.ToString());
            }
        }, tokenSource.Token);
        streamPeer.StartServe(tokenSource);
        var handshake = await _handshakeProvider.GetHandshakeAsync();
        var handShakeReply = await streamPeer.HandShakeAsync(new HandshakeRequest { Handshake = handshake });
        if (!await ProcessHandshakeReplyAsync(handShakeReply, streamPeer.RemoteEndpoint))
        {
            await streamPeer.DisconnectAsync(true);
            return false;
        }

        streamPeer.InboundSessionId = handshake.SessionId.ToByteArray();
        streamPeer.Info.SessionId = handShakeReply.Handshake.SessionId.ToByteArray();
        Logger.LogInformation("streaming Handshake to {remoteEndPoint} successful.sessionInfo {InboundSessionId} {SessionId}", streamPeer.RemoteEndpoint.ToString(), streamPeer.InboundSessionId.ToHex(), streamPeer.Info.SessionId.ToHex());
        return true;
    }


    /// <summary>
    ///     Checks that the distant node is reachable by pinging it.
    /// </summary>
    /// <returns>The reply from the server.</returns>
    private async Task PingNodeAsync(GrpcClient client, DnsEndPoint peerEndpoint)
    {
        try
        {
            var metadata = new Metadata
            {
                { GrpcConstants.RetryCountMetadataKey, "0" },
                { GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeout.ToString() }
            };

            await client.Client.PingAsync(new PingRequest(), metadata);

            Logger.LogDebug($"Pinged {peerEndpoint} successfully.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Could not ping {peerEndpoint}.");
            await client.Channel.ShutdownAsync();
            throw;
        }
    }

    /// <summary>
    ///     Creates a channel/client pair with the appropriate options and interceptors.
    /// </summary>
    /// <returns>A tuple of the channel and client</returns>
    private async Task<GrpcClient> CreateClientAsync(DnsEndPoint remoteEndpoint)
    {
        var certificate = await RetrieveServerCertificateAsync(remoteEndpoint);

        if (certificate == null)
            return null;

        Logger.LogDebug($"Upgrading connection to TLS: {certificate}.");
        var clientKeyCertificatePair = TlsHelper.GenerateKeyCertificatePair();
        ChannelCredentials credentials =
            new SslCredentials(TlsHelper.ObjectToPem(certificate), clientKeyCertificatePair);

        var channel = new Channel(remoteEndpoint.ToString(), credentials, new List<ChannelOption>
        {
            new(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
            new(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength),
            new(ChannelOptions.SslTargetNameOverride, GrpcConstants.DefaultTlsCommonName),
            new(GrpcConstants.GrpcArgKeepalivePermitWithoutCalls, GrpcConstants.GrpcArgKeepalivePermitWithoutCallsOpen),
            new(GrpcConstants.GrpcArgHttp2MaxPingsWithoutData, GrpcConstants.GrpcArgHttp2MaxPingsWithoutDataVal),
            new(GrpcConstants.GrpcArgKeepaliveTimeoutMs, GrpcConstants.GrpcArgKeepaliveTimeoutMsVal),
            new(GrpcConstants.GrpcArgKeepaliveTimeMs, GrpcConstants.GrpcArgKeepaliveTimeMsVal),
        });

        var nodePubkey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex();

        var interceptedChannel = channel.Intercept(metadata =>
        {
            metadata.Add(GrpcConstants.PubkeyMetadataKey, nodePubkey);
            return metadata;
        }).Intercept(new RetryInterceptor());

        var client = new PeerService.PeerServiceClient(interceptedChannel);
        return new GrpcClient(channel, client, certificate);
    }

    private async Task<X509Certificate> RetrieveServerCertificateAsync(DnsEndPoint remoteEndpoint)
    {
        Logger.LogDebug($"Starting certificate retrieval for {remoteEndpoint}.");

        TcpClient client = null;

        try
        {
            client = new TcpClient();
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(NetworkConstants.DefaultSslCertifFetchTimeout);
                await client.ConnectAsync(remoteEndpoint.Host, remoteEndpoint.Port).WithCancellation(cts.Token);

                using (var sslStream = new SslStream(client.GetStream(), true, (a, b, c, d) => true))
                {
                    sslStream.ReadTimeout = NetworkConstants.DefaultSslCertifFetchTimeout;
                    sslStream.WriteTimeout = NetworkConstants.DefaultSslCertifFetchTimeout;
                    await sslStream.AuthenticateAsClientAsync(remoteEndpoint.Host).WithCancellation(cts.Token);

                    if (sslStream.RemoteCertificate == null)
                    {
                        Logger.LogDebug($"Certificate from {remoteEndpoint} is null");
                        return null;
                    }

                    Logger.LogDebug($"Retrieved certificate for {remoteEndpoint}.");

                    return FromX509Certificate(sslStream.RemoteCertificate);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug($"Certificate retrieval connection timeout for {remoteEndpoint}.");
            return null;
        }
        catch (Exception ex)
        {
            // swallow exception because it's currently not a hard requirement to 
            // upgrade the connection.
            Logger.LogWarning(ex, $"Could not retrieve certificate from {remoteEndpoint}.");
        }
        finally
        {
            client?.Close();
        }

        return null;
    }

    public static X509Certificate FromX509Certificate(
        System.Security.Cryptography.X509Certificates.X509Certificate x509Cert)
    {
        return new X509CertificateParser().ReadCertificate(x509Cert.GetRawCertData());
    }
}