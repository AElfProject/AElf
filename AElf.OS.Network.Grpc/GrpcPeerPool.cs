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
            try
            {
                var peer = _authenticatedPeers.FirstOrDefault(p => p.Key == address).Value;

                if (peer == null)
                {
                    Logger.LogWarning($"Could not find peer {address}.");
                    return false;
                }

                await peer.SendDisconnectAsync();
                await peer.StopAsync();

                return _authenticatedPeers.TryRemove(peer.PeerAddress, out _);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while removing peer {address}.");
                return false;
            }
        }

        private async Task<bool> DialAsync(string address)
        {
            try
            {
                Logger.LogTrace($"Attempting to reach {address}.");

                var splitAddress = address.Split(":");
                Channel channel = new Channel(splitAddress[0], int.Parse(splitAddress[1]), ChannelCredentials.Insecure);

                var client = new PeerService.PeerServiceClient(channel);
                var hsk = await BuildHandshakeAsync();

                if (channel.State == ChannelState.TransientFailure)
                {
                    // if failing give it some time to recover
                    await channel.TryWaitForStateChangedAsync(channel.State,
                        DateTime.UtcNow.AddSeconds(_networkOptions.PeerDialTimeout));
                }

                AuthResponse resp = null;
                
                try
                {
                    resp = await client.ConnectAsync(hsk,
                        new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(_networkOptions.PeerDialTimeout)));
                }
                catch (Exception e)
                {
                    // cleanup essentially for testing
                    await channel.ShutdownAsync();
                }

                // todo refactor so that connect returns the handshake and we'll check here 
                // todo if not correct we kill the channel. 

                if (resp == null || resp.Success != true)
                    return false;

                var peer = new GrpcPeer(channel, client, null, address, resp.Port);
                
                peer.DisconnectionEvent += PeerOnDisconnectionEvent;
                _authenticatedPeers[address] = peer;

                Logger.LogTrace($"Connected to {address}.");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while connection to {address}.");
                return false;
            }
        }

        private void PeerOnDisconnectionEvent(object sender, EventArgs e)
        {
            if (sender is GrpcPeer p && _authenticatedPeers.TryRemove(p.PeerAddress, out GrpcPeer removed))
            {
                removed.DisconnectionEvent -= PeerOnDisconnectionEvent;
                Logger.LogDebug($"Removed peer {removed.PublicKey.ToHex()} - {removed}");
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
                .Where(p => p.Value.PeerAddress == peerAddress || peerAddress.EndsWith(p.Value.RemoteEndpoint))
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public IPeer FindPeerByPublicKey(byte[] publicKey)
        {
            return _authenticatedPeers.Where(p => publicKey.BytesEqual(p.Value.PublicKey))
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public bool IsAuthenticatePeer(string peerAddress, Handshake handshake)
        {
            var pubKey = handshake.HskData.PublicKey.ToByteArray();
            if (pubKey.BytesEqual(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync)))
                return false;

            var alreadyConnected = _authenticatedPeers
                .Where(p => p.Value.PeerAddress == peerAddress || pubKey.BytesEqual(p.Value.PublicKey))
                .Select(p => p.Value)
                .FirstOrDefault();

            if (alreadyConnected != null)
                return false;

            return true;
        }

        public bool AddPeer(IPeer peer)
        {
            if (!(peer is GrpcPeer p)) 
                return false;
            
            _authenticatedPeers[peer.PeerAddress] = p;
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

        public async Task ProcessDisconnection(string peerEndpoint)
        {
            var p = _authenticatedPeers.FirstOrDefault(pr => pr.Value.RemoteEndpoint == peerEndpoint);
            
            if (_authenticatedPeers.TryRemove(p.Key, out GrpcPeer peer))
            {
                await peer.StopAsync();
            }
        }
    }
}