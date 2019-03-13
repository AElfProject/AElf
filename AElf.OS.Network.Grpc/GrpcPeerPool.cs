using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeerPool : IPeerPool
    {
        private readonly NetworkOptions _networkOptions;

        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        private readonly ConcurrentDictionary<string, GrpcPeer> _authenticatedPeers;

        public ILogger<GrpcPeerPool> Logger { get; set; }

        public GrpcPeerPool(IOptionsSnapshot<NetworkOptions> networkOptions, IAccountService accountService, 
            IBlockchainService blockChainService)
        {
            _networkOptions = networkOptions.Value;
            _accountService = accountService;
            _blockchainService = blockChainService;

            _authenticatedPeers = new ConcurrentDictionary<string, GrpcPeer>();

            Logger = NullLogger<GrpcPeerPool>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            if (FindPeerByAddress(address) != null)
                return false;

            return await DialAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            var peer = _authenticatedPeers.FirstOrDefault(p => p.Value.PeerIpAddress == address).Value;

            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer {address}.");
                return false;
            }

            try
            {
                await peer.SendDisconnectAsync();
            }
            catch (RpcException e)
            {
                Logger.LogError(e, $"Error sending disconnect peer {address}.");
            }
            
            // todo factor
            await peer.StopAsync();

            return _authenticatedPeers.TryRemove(peer.PubKey, out _);
        }

        private async Task<bool> DialAsync(string ipAddress)
        {
            Logger.LogTrace($"Attempting to reach {ipAddress}.");

            Channel channel = new Channel(ipAddress, ChannelCredentials.Insecure);

            var client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
            {
                metadata.Add(GrpcConsts.PubkeyMetadataKey, AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex());
                return metadata;
            }));
            
            var hsk = await BuildHandshakeAsync();

            if (channel.State == ChannelState.TransientFailure)
            {
                // if failing give it some time to recover
                await channel.TryWaitForStateChangedAsync(channel.State,
                    DateTime.UtcNow.AddSeconds(_networkOptions.PeerDialTimeout));
            }

            ConnectReply connectReply;
            
            try
            {
                connectReply = await client.ConnectAsync(hsk,
                    new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(_networkOptions.PeerDialTimeout)));
            }
            catch (RpcException e)
            {
                await channel.ShutdownAsync();
                Logger.LogError(e, $"Could not connect to {ipAddress}.");
                return false;
            }

            // todo refactor so that connect returns the handshake and we'll check here 
            // todo if not correct we kill the channel.
            if (connectReply?.Handshake?.HskData == null || connectReply.Err != AuthError.None)
            {
                Logger.LogWarning($"Incorrect handshake for {ipAddress}.");
                await channel.ShutdownAsync();
                return false;
            }

            // todo injector 
            var pubKey = connectReply.Handshake.HskData.PublicKey.ToHex();
            var peer = new GrpcPeer(channel, client, pubKey, ipAddress);

            if (!_authenticatedPeers.TryAdd(pubKey, peer))
            {
                Logger.LogWarning($"Peer {pubKey} is already in list.");
                await peer.StopAsync();
                return false;
            }
            
            peer.DisconnectionEvent += PeerOnDisconnectionEvent;

            Logger.LogTrace($"Connected to {pubKey} ({ipAddress}).");

            return true;
        }

        private void PeerOnDisconnectionEvent(object sender, EventArgs e)
        {
            if (!(sender is GrpcPeer p)) 
                return;
            
            if (_authenticatedPeers.TryRemove(p.PubKey, out GrpcPeer removed))
            {
                removed.DisconnectionEvent -= PeerOnDisconnectionEvent;
                Logger.LogDebug($"Removed peer {removed.PubKey} - {removed}");
            }
            else
            {
                Logger.LogDebug($"Removed peer {p.PubKey}");
            }
        }

        public List<IPeer> GetPeers(bool includeFailing = false)
        {
            var peers = _authenticatedPeers.Select(p => p.Value);

            if (!includeFailing)
                peers = peers.Where(p => p.IsReady);

            return peers.Select(p => p as IPeer).ToList();
        }

        public IPeer FindPeerByAddress(string peerAddress)
        {
            return _authenticatedPeers
                .Where(p => p.Value.PeerIpAddress == peerAddress)
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(string publicKey)
        {
            _authenticatedPeers.TryGetValue(publicKey, out GrpcPeer p);
            return p;
        }

        public bool IsAuthenticatePeer(string remotePubKey)
        {
            string localPubKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();
            
            if (remotePubKey == localPubKey)
                return false;

            return FindPeerByPublicKey(remotePubKey) == null;
        }

        public bool AddPeer(IPeer peer)
        {
            if (!(peer is GrpcPeer p)) 
                return false;
            
            if (!_authenticatedPeers.TryAdd(p.PubKey, p))
            {
                Logger.LogWarning($"Could not add peer {peer.PubKey} ({peer.PeerIpAddress})");
                return false;
            }
            
            p.DisconnectionEvent += PeerOnDisconnectionEvent;
            
            return true;
        }

        public async Task<Handshake> GetHandshakeAsync()
        {
            return await BuildHandshakeAsync();
        }

        private async Task<Handshake> BuildHandshakeAsync()
        {
            var nd = new HandshakeData
            {
                ListeningPort = _networkOptions.ListeningPort,
                PublicKey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()),
                Version = ChainConsts.ProtocolVersion,
            };

            byte[] sig = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());

            var hsk = new Handshake
            {
                HskData = nd,
                Sig = ByteString.CopyFrom(sig),
                Header = await _blockchainService.GetBestChainLastBlock()
            };

            return hsk;
        }

        public async Task ProcessDisconnection(string pubKey)
        {            
            if (_authenticatedPeers.TryRemove(pubKey, out GrpcPeer peer))
            {
                // todo factor
                peer.DisconnectionEvent -= PeerOnDisconnectionEvent;
                await peer.StopAsync();
            }
        }
    }
}