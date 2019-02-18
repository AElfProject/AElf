using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Services;
using AElf.OS.Network.Grpc.Events;
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
        
        private readonly List<GrpcPeer> _authenticatedPeers;
        
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        private int ChainId
        {
            get { return _chainOptions.ChainId.ConvertBase58ToChainId(); }
        }
        
        public GrpcPeerPool(IOptionsSnapshot<ChainOptions> chainOptions, IOptionsSnapshot<NetworkOptions> networkOptions, 
            IAccountService accountService, IBlockchainService blockChainService)
        {
            _networkOptions = networkOptions.Value;
            _accountService = accountService;
            
            _authenticatedPeers = new List<GrpcPeer>();
            
            Logger = NullLogger<GrpcNetworkServer>.Instance;
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
                GrpcPeer peer = _authenticatedPeers.FirstOrDefault(p => p.PeerAddress == address);
            
                if (peer == null)
                {
                    Logger.LogWarning($"Could not find peer {address}.");
                    return false;
                }

                await peer.SendDisconnectAsync();
                await peer.StopAsync();
                
                return _authenticatedPeers.Remove(peer);
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
                
                if (channel.State != ChannelState.Connecting)
                {
                    await channel.TryWaitForStateChangedAsync(channel.State, DateTime.UtcNow.AddSeconds(_dialTimeout));
                }
                
                var resp = await client.ConnectAsync(hsk, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(_dialTimeout)));
                
                // todo refactor so that connect returns the handshake and we'll check here 
                // todo if not correct we kill the channel. 

                if (resp.Success != true)
                    return false;

                _authenticatedPeers.Add(new GrpcPeer(channel, client, null, address, resp.Port)); 
                        
                Logger.LogTrace($"Connected to {address}.");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while connection to {address}.");
                return false;
            }
        }

        public List<GrpcPeer> GetPeers()
        {
            return _authenticatedPeers.ToList();
        }

        public GrpcPeer FindPeer(string peerEndpoint, byte[] publicKey = null)
        {
            if (string.IsNullOrWhiteSpace(peerEndpoint) && publicKey == null)
                throw new InvalidOperationException("address and public cannot be both null.");

            IEnumerable<GrpcPeer> toFind = _authenticatedPeers;

            if (!string.IsNullOrWhiteSpace(peerEndpoint))
                toFind = toFind.Where(p => p.PeerAddress == peerEndpoint);

            if (publicKey != null)
                toFind = toFind.Where(p => publicKey.BytesEqual(p.PublicKey));
            
            return toFind.FirstOrDefault();
        }

        public bool AuthenticatePeer(string peerEndpoint, byte[] pubkey, Handshake handshake)
        {
            if (pubkey.BytesEqual(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync)))
                return false;
            
             bool alreadyConnected = _authenticatedPeers.FirstOrDefault(p => p.PeerAddress == peerEndpoint || pubkey.BytesEqual(p.PublicKey)) != null;

             if (alreadyConnected)
                 return false;
             
             // todo check handshake
             
            return true;
        }

        public bool AddPeer(GrpcPeer peer)
        {
            _authenticatedPeers.Add(peer);
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
                Version = GlobalConfig.ProtocolVersion,
            };
            
            byte[] sig = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());

            var hsk = new Handshake
            {
                HskData = nd,
                Sig = ByteString.CopyFrom(sig),
                Header = await _blockchainService.GetBestChainLastBlock(ChainId)
            };

            return hsk;
        }
        
        public void ProcessDisconnection(string peer)
        {
            _authenticatedPeers.RemoveAll(p => p.RemoteListenPort == peer);
        }
    }
}