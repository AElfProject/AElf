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
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.X509;
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

    private KeyCertificatePair _clientKeyCertificatePair;

    public PeerDialer(IAccountService accountService,
        IHandshakeProvider handshakeProvider)
    {
        _accountService = accountService;
        _handshakeProvider = handshakeProvider;

        Logger = NullLogger<PeerDialer>.Instance;

        CreateClientKeyCertificatePair();
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

        // verify handshake
        if (handshakeReply.Error != HandshakeError.HandshakeOk)
        {
            Logger.LogWarning($"Handshake error: {remoteEndpoint} {handshakeReply.Error}.");
            await client.Channel.ShutdownAsync();
            return null;
        }

        if (await _handshakeProvider.ValidateHandshakeAsync(handshakeReply.Handshake) !=
            HandshakeValidationResult.Ok)
        {
            Logger.LogWarning($"Connect error: {remoteEndpoint} {handshakeReply}.");
            await client.Channel.ShutdownAsync();
            return null;
        }

        var peer = new GrpcPeer(client, remoteEndpoint, new PeerConnectionInfo
        {
            Pubkey = handshakeReply.Handshake.HandshakeData.Pubkey.ToHex(),
            ConnectionTime = TimestampHelper.GetUtcNow(),
            ProtocolVersion = handshakeReply.Handshake.HandshakeData.Version,
            SessionId = handshakeReply.Handshake.SessionId.ToByteArray(),
            IsInbound = false,
            NodeVersion = handshakeReply.Handshake.HandshakeData.NodeVersion
        });

        peer.UpdateLastReceivedHandshake(handshakeReply.Handshake);

        peer.InboundSessionId = handshake.SessionId.ToByteArray();
        peer.UpdateLastSentHandshake(handshake);

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

    private void CreateClientKeyCertificatePair()
    {
        _clientKeyCertificatePair = TlsHelper.GenerateKeyCertificatePair();
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
        ChannelCredentials credentials =
            new SslCredentials(TlsHelper.ObjectToPem(certificate), _clientKeyCertificatePair);

        var channel = new Channel(remoteEndpoint.ToString(), credentials, new List<ChannelOption>
        {
            new(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
            new(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength),
            new(ChannelOptions.SslTargetNameOverride, GrpcConstants.DefaultTlsCommonName)
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