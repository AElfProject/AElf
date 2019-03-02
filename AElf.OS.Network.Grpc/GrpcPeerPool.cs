using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeerPool : IPeerPool
    {
        private readonly int _dialTimeout;
            
        private readonly NetworkOptions _networkOptions;
        private readonly ChainOptions _chainOptions;
        
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        
        private readonly ConcurrentDictionary<string, GrpcPeer> _authenticatedPeers;
        
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcPeerPool> Logger { get; set; }
        
        private int ChainId => _chainOptions.ChainId;

        public GrpcPeerPool(IOptionsSnapshot<ChainOptions> chainOptions, IOptionsSnapshot<NetworkOptions> networkOptions, 
            IAccountService accountService, IBlockchainService blockChainService)
        {
            _networkOptions = networkOptions.Value;
            _accountService = accountService;
            
            _authenticatedPeers = new ConcurrentDictionary<string, GrpcPeer>();
            
            Logger = NullLogger<GrpcPeerPool>.Instance;
            EventBus = NullLocalEventBus.Instance;

            _dialTimeout = networkOptions.Value.PeerDialTimeout ?? NetworkConsts.DefaultPeerDialTimeout;

            _chainOptions = chainOptions.Value;

            _blockchainService = blockChainService;
        }
        
        public async Task<bool> AddPeerAsync(string address)
        {
            if (FindPeer(address) != null)
                return false;
            
            return await Dial(address);
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
        
        private async Task<bool> Dial(string address)
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
                    await channel.TryWaitForStateChangedAsync(channel.State, DateTime.UtcNow.AddSeconds(_dialTimeout));
                }
                
                var resp = await client.ConnectAsync(hsk, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(_dialTimeout)));
                
                // todo refactor so that connect returns the handshake and we'll check here 
                // todo if not correct we kill the channel. 

                if (resp.Success != true)
                    return false;

                _authenticatedPeers[address] = new GrpcPeer(channel, client, null, address, resp.Port); 
                                        
                Logger.LogTrace($"Connected to {address}.");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while connection to {address}.");
                return false;
            }
        }

        public List<IPeer> GetPeers()
        {
            return _authenticatedPeers.Values.Select(p => p as IPeer).ToList();
        }

        public IPeer FindPeer(string peerAddress, byte[] publicKey = null)
        {
            if (string.IsNullOrWhiteSpace(peerAddress) && publicKey == null)
                throw new InvalidOperationException("address and public cannot be both null.");
            
            IEnumerable<KeyValuePair<string, GrpcPeer>> toFind = _authenticatedPeers;

            if (!string.IsNullOrWhiteSpace(peerAddress))
                toFind = toFind.Where(p => p.Value.PeerAddress == peerAddress || peerAddress.EndsWith(p.Value.RemoteEndpoint));

            if (publicKey != null)
                toFind = toFind.Where(p => publicKey.BytesEqual(p.Value.PublicKey));

            return toFind.FirstOrDefault().Value;
        }

        public bool AuthenticatePeer(string peerAddress, Handshake handshake)
        {
            var pubKey = handshake.HskData.PublicKey.ToByteArray();
            if (pubKey.BytesEqual(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync)))
                return false;
            
            var alreadyConnected = _authenticatedPeers.FirstOrDefault(p => p.Value.PeerAddress == peerAddress || pubKey.BytesEqual(p.Value.PublicKey));

            if (!alreadyConnected.Equals(default(KeyValuePair<string, GrpcPeer>)))
                return false;
             
            return true;
        }

        public bool AddPeer(IPeer peer)
        {
            _authenticatedPeers[peer.PeerAddress] = peer as GrpcPeer;
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
        
        public void ProcessDisconnection(string peerEndpoint)
        {
            _authenticatedPeers.RemoveAll(p => p.Value.RemoteEndpoint == peerEndpoint);
        }
    }
}