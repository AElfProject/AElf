using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeerPool : IPeerPool
    {
        private readonly NetworkOptions _networkOptions;

        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        private readonly ConcurrentDictionary<string, GrpcPeer> _authenticatedPeers;

        public ILocalEventBus EventBus { get; set; }
        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }

        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;

        public ILogger<GrpcPeerPool> Logger { get; set; }

        public GrpcPeerPool(IOptionsSnapshot<NetworkOptions> networkOptions, IAccountService accountService, 
            IBlockchainService blockChainService)
        {
            _networkOptions = networkOptions.Value;
            _accountService = accountService;
            _blockchainService = blockChainService;

            _authenticatedPeers = new ConcurrentDictionary<string, GrpcPeer>();
            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);

            Logger = NullLogger<GrpcPeerPool>.Instance;
        }

        public int PeerCount => _authenticatedPeers.Count;

        public async Task<bool> AddPeerAsync(string address)
        {
            if (FindPeerByAddress(address) != null)
                return false;

            return await DialAsync(address);
        }
        
        private async Task<bool> DialAsync(string ipAddress)
        {
            Logger.LogTrace($"Attempting to reach {ipAddress}.");

            var (channel, client) = await CreateClientAsync(ipAddress);

            var handshake = await BuildHandshakeAsync();
            ConnectReply connectReply = await TryConnectAsync(client, ipAddress, handshake);

            if (connectReply == null)
            {
                await channel.ShutdownAsync();
                return false;
            }
            
            var pubKey = connectReply.Handshake.HandshakeData.Pubkey.ToHex();
            
            var connectionInfo = new PeerInfo 
            { 
                Pubkey = pubKey, 
                IpAddress = ipAddress,
                ProtocolVersion = connectReply.Handshake.HandshakeData.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = connectReply.Handshake.BestChainBlockHeader.Height,
                LibHeightAtHandshake = connectReply.Handshake.LibBlockHeight
            };

            var peer = new GrpcPeer(channel, client, connectionInfo);

            if (!_authenticatedPeers.TryAdd(pubKey, peer))
            {
                Logger.LogWarning($"Peer {pubKey} is already in list.");
                await channel.ShutdownAsync();
                return false;
            }

            var finalizeReply = await peer.FinalizeConnectAsync(handshake);
            
            if (finalizeReply == null || !finalizeReply.Success)
            {
                Logger.LogWarning($"Could not finalize connection to {ipAddress} - {pubKey}");
                await RemovePeerAsync(pubKey, true); // remove and cleanup
                return false;
            }
            
            Logger.LogTrace($"Connected to {peer} -- height {peer.Info.StartHeight}.");
            
            FireConnectionEvent(connectReply, pubKey);

            return true;
        }

        private void FireConnectionEvent(ConnectReply connectReply, string pubKey)
        {
            _ = EventBus.PublishAsync(new AnnouncementReceivedEventData(new BlockAnnouncement
            {
                BlockHash = connectReply.Handshake.BestChainBlockHeader.GetHash(),
                BlockHeight = connectReply.Handshake.BestChainBlockHeader.Height
            }, pubKey));
        }
        
        private async Task<ConnectReply> TryConnectAsync(PeerService.PeerServiceClient client, string ipAddress, Handshake handshake)
        {
            ConnectReply connectReply;
            
            try
            {
                Metadata data = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, _networkOptions.PeerDialTimeoutInMilliSeconds.ToString()}};
                
                connectReply = await client.ConnectAsync(handshake, data);
            }
            catch (AggregateException e)
            {
                Logger.LogError(e, $"Could not connect to {ipAddress}.");
                return null;
            }
            
            if (connectReply?.Handshake?.HandshakeData == null || connectReply.Error != AuthError.None)
            {
                Logger.LogWarning($"Incorrect handshake for {ipAddress}, {connectReply?.Error}.");
                return null;
            }

            return connectReply;
        }

        private async Task<(Channel, PeerService.PeerServiceClient)> CreateClientAsync(string ipAddress)
        {
            Channel channel = new Channel(ipAddress, ChannelCredentials.Insecure, new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength)
            });
            
            var client = new PeerService.PeerServiceClient(channel
                .Intercept(metadata =>
                {
                    metadata.Add(GrpcConstants.PubkeyMetadataKey, AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex());
                    return metadata;
                })
                .Intercept(new RetryInterceptor()));
            
            if (channel.State == ChannelState.TransientFailure)
            {
                // if failing give it some time to recover
                await channel.TryWaitForStateChangedAsync(channel.State,
                    DateTime.UtcNow.AddSeconds(_networkOptions.PeerDialTimeoutInMilliSeconds));
            }

            return (channel, client);
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
                .Where(p => p.Value.IpAddress == peerAddress)
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
                return null;
            
            _authenticatedPeers.TryGetValue(publicKey, out GrpcPeer p);
            
            return p;
        }

        public IPeer GetBestPeer()
        {
            return GetPeers().FirstOrDefault(p => p.IsBest);
        }

        public bool AddPeer(IPeer peer)
        {
            if (!(peer is GrpcPeer p))
                return false;
            
            string localPubKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();

            if (peer.Pubkey == localPubKey)
                throw new InvalidOperationException($"Connection to self detected {peer.Pubkey} ({peer.IpAddress})");

            if (!_authenticatedPeers.TryAdd(p.Pubkey, p))
            {
                Logger.LogWarning($"Could not add peer {peer.Pubkey} ({peer.IpAddress})");
                return false;
            }
            
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
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()),
                Version = KernelConstants.ProtocolVersion,
                ChainId = _blockchainService.GetChainId()
            };

            byte[] sig = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());

            var chain = await _blockchainService.GetChainAsync();
                
            var hsk = new Handshake
            {
                HandshakeData = nd,
                Signature = ByteString.CopyFrom(sig),
                BestChainBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync(),
                LibBlockHeight = chain?.LastIrreversibleBlockHeight ?? 0
            };

            return hsk;
        }

        public async Task<bool> RemovePeerByAddressAsync(string address)
        {
            var peer = _authenticatedPeers.FirstOrDefault(p => p.Value.IpAddress == address).Value;

            if (peer != null) 
                return await RemovePeerAsync(peer.Pubkey, true) != null;
            
            Logger.LogWarning($"Could not find peer {address}.");
            
            return false;
        }
        
        public async Task<IPeer> RemovePeerAsync(string publicKey, bool sendDisconnect)
        {
            if (_authenticatedPeers.TryRemove(publicKey, out GrpcPeer removed))
            {
                await removed.DisconnectAsync(sendDisconnect);
                Logger.LogDebug($"Removed peer {removed}");
            }
            else
            {
                Logger.LogDebug($"Could not find {publicKey}");
            }

            return removed;
        }

        public async Task ClearAllPeersAsync(bool sendDisconnect)
        {
            var peersToRemove = _authenticatedPeers.Keys.ToList();
            
            foreach (string peer in peersToRemove)
            {
                await RemovePeerAsync(peer, sendDisconnect);
            }
        }
        
        public void AddRecentBlockHeightAndHash(long blockHeight,Hash blockHash, bool hasFork)
        {
            _recentBlockHeightAndHashMappings[blockHeight] = blockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }
    }
}