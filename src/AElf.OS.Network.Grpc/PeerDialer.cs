using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Provides functionality to setup a connection to a distant node by exchanging some
    /// low level information.
    /// </summary>
    public class PeerDialer : IPeerDialer
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }

        private readonly IAccountService _accountService;
        private readonly IHandshakeProvider _handshakeProvider;

        public ILogger<PeerDialer> Logger { get; set; }

        private KeyCertificatePair _clientKeyCertificatePair;

        public PeerDialer(IAccountService accountService,
            IHandshakeProvider handshakeProvider)
        {
            _accountService = accountService;
            _handshakeProvider = handshakeProvider;

            Logger = NullLogger<PeerDialer>.Instance;

            CreateClientKeyCertificatePair();
        }

        private void CreateClientKeyCertificatePair()
        {
            var commonCertifName = "CN=" + GrpcConstants.DefaultTlsCommonName;
            
            var rsaKeyPair = TlsHelper.GenerateRsaKeyPair();
            var clientCertificate = TlsHelper.GenerateCertificate(new X509Name(commonCertifName),
                new X509Name(commonCertifName), rsaKeyPair.Private, rsaKeyPair.Public);
            
            _clientKeyCertificatePair = new KeyCertificatePair(TlsHelper.ObjectToPem(clientCertificate), TlsHelper.ObjectToPem(rsaKeyPair.Private));
        }

        /// <summary>
        /// Given an IP address, will create a handshake to the distant node for
        /// further communications.
        /// </summary>
        /// <returns>The created peer</returns>
        public async Task<GrpcPeer> DialPeerAsync(DnsEndPoint remoteEndpoint)
        {
            var client = await CreateClientAsync(remoteEndpoint);
            
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
                IsSecure = client.IsSecure
            });

            peer.UpdateLastReceivedHandshake(handshakeReply.Handshake);
            
            peer.InboundSessionId = handshake.SessionId.ToByteArray();
            peer.UpdateLastSentHandshake(handshake);

            return peer;
        }

        /// <summary>
        /// Calls the server side DoHandshake RPC method, in order to establish a 2-way connection.
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
                    {GrpcConstants.RetryCountMetadataKey, "0"},
                    {GrpcConstants.TimeoutMetadataKey, (NetworkOptions.PeerDialTimeout * 2).ToString()}
                };

                handshakeReply =
                    await client.Client.DoHandshakeAsync(new HandshakeRequest {Handshake = handshake}, metadata);
                
                Logger.LogDebug($"Handshake to {remoteEndPoint} successful.");
            }
            catch (Exception)
            {
                await client.Channel.ShutdownAsync();
                throw;
            }

            return handshakeReply;
        }

        public async Task<GrpcPeer> DialBackPeerAsync(DnsEndPoint remoteEndpoint, Handshake handshake)
        {
            var client = await CreateClientAsync(remoteEndpoint);
            await PingNodeAsync(client, remoteEndpoint);

            var peer = new GrpcPeer(client, remoteEndpoint, new PeerConnectionInfo
            {
                Pubkey = handshake.HandshakeData.Pubkey.ToHex(),
                ConnectionTime = TimestampHelper.GetUtcNow(),
                SessionId = handshake.SessionId.ToByteArray(),
                ProtocolVersion = handshake.HandshakeData.Version,
                IsInbound = true,
                IsSecure = client.IsSecure
            });

            peer.UpdateLastReceivedHandshake(handshake);

            return peer;
        }

        /// <summary>
        /// Checks that the distant node is reachable by pinging it.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task PingNodeAsync(GrpcClient client, DnsEndPoint peerEndpoint)
        {
            try
            {
                var metadata = new Metadata
                {
                    {GrpcConstants.RetryCountMetadataKey, "0"},
                    {GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeout.ToString()}
                };

                await client.Client.PingAsync(new PingRequest(), metadata);

                Logger.LogDebug($"Pinged {peerEndpoint} successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogInformation(ex, $"Could not ping {peerEndpoint}.");
                await client.Channel.ShutdownAsync();
                throw;
            }
        }

        /// <summary>
        /// Creates a channel/client pair with the appropriate options and interceptors.
        /// </summary>
        /// <returns>A tuple of the channel and client</returns>
        private async Task<GrpcClient> CreateClientAsync(DnsEndPoint remoteEndpoint)
        {
            var certificate = await RetrieveServerCertificateAsync(remoteEndpoint);

            ChannelCredentials credentials = ChannelCredentials.Insecure;

            if (certificate != null)
            {
                Logger.LogDebug($"Upgrading connection to TLS: {certificate}.");
                credentials =  new SslCredentials(TlsHelper.ObjectToPem(certificate), _clientKeyCertificatePair);
            }
            
            var channel = new Channel(remoteEndpoint.ToString(), credentials, new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength),
                new ChannelOption(ChannelOptions.SslTargetNameOverride, GrpcConstants.DefaultTlsCommonName)
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
                client.ConnectAsync(remoteEndpoint.Host, remoteEndpoint.Port)
                    .Wait(NetworkConstants.DefaultSslCertifFetchTimeout);

                using (var sslStream = new SslStream(client.GetStream(), true, (a, b, c, d) => true))
                {
                    sslStream.ReadTimeout = NetworkConstants.DefaultSslCertifFetchTimeout;
                    sslStream.WriteTimeout = NetworkConstants.DefaultSslCertifFetchTimeout;
                    await sslStream.AuthenticateAsClientAsync(remoteEndpoint.Host);

                    if (sslStream.RemoteCertificate == null)
                    {
                        Logger.LogDebug($"Certificate from {remoteEndpoint} is null");
                        return null;
                    }

                    Logger.LogDebug($"Retrieved certificate for {remoteEndpoint}.");

                    return FromX509Certificate(sslStream.RemoteCertificate);
                }
            }
            catch (Exception ex)
            {
                // swallow exception because it's currently not a hard requirement to 
                // upgrade the connection.
                Logger.LogInformation(ex, $"Could not retrieve certificate from {remoteEndpoint}.");
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
}