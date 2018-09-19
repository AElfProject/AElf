using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;
using ClientBase = AElf.Miner.Rpc.Client.ClientBase;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner
{
    [LoggerName("MinerClient")]
    public class ClientManager
    {
        private readonly Dictionary<string, ClientToSideChain> _clientsToSideChains =
            new Dictionary<string, ClientToSideChain>();

        private ClientToParentChain _clientToParentChain;

        private CertificateStore _certificateStore;
        private readonly ILogger _logger;
        private readonly IChainManagerBasic _chainManagerBasic;
        private Dictionary<string, Uri> ChildChains => GrpcRemoteConfig.Instance.ChildChains;
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;
        private int _interval;

        /// <summary>
        /// Waiting interval for taking element.
        /// </summary>
        private int Interval => GrpcLocalConfig.Instance.WaitingIntervalInMillisecond;

        public ClientManager(ILogger logger, IChainManagerBasic chainManagerBasic)
        {
            _logger = logger;
            _chainManagerBasic = chainManagerBasic;
            GrpcRemoteConfig.ConfigChanged += GrpcRemoteConfigOnConfigChanged;
        }

        private void GrpcRemoteConfigOnConfigChanged(object sender, EventArgs e)
        {
            _tokenSourceToSideChain?.Cancel();
            _tokenSourceToSideChain?.Dispose();
            _tokenSourceToParentChain?.Cancel();
            _tokenSourceToParentChain?.Dispose();

            // reset
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();

            // client cache would be cleared since configuration has been changed
            // Todo: only clear clients which is needed
            _clientsToSideChains.Clear();
            _clientToParentChain = null;
            Init();
        }

        /// <summary>
        /// Initialize client manager.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="interval"></param>
        public void Init(string dir = "", int interval = 0)
        {
            if (!GrpcLocalConfig.Instance.Client) return;
            _certificateStore = dir == "" ? _certificateStore : new CertificateStore(dir);
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();
            _interval = interval == 0 ? Globals.AElfMiningInterval : interval;
            CreateClientsToSideChain();
            CreateClientToParentChain();
        }

        /// <summary>
        /// Create multi clients for different side chains
        /// this would be invoked when miner starts or configuration reloaded 
        /// </summary>
        /// <returns></returns>
        private async Task CreateClientsToSideChain()
        {
            if (!GrpcLocalConfig.Instance.Client)
                return;

            _clientsToSideChains.Clear();
            foreach (var sideChainId in ChildChains.Keys)
            {
                var client = CreateClientToSideChain(sideChainId);
                var height =
                    await _chainManagerBasic.GetCurrentBlockHeightsync(ByteArrayHelpers.FromHexString(sideChainId));

                // keep-alive
                client.StartDuplexStreamingCall(_tokenSourceToSideChain.Token, height);
            }
        }

        /// <summary>
        /// Start a new client to the side chain
        /// </summary>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        public ClientToSideChain CreateClientToSideChain(string targetChainId)
        {
            // NOTE: do not use cache if configuration is managed by cluster
            //if (_clientsToSideChains.TryGetValue(targetChainId, out var clientToSideChain)) return clientToSideChain;
            if (!ChildChains.TryGetValue(targetChainId, out var chainUri))
                throw new ChainInfoNotFoundException($"Unable to get chain Info of {targetChainId}.");
            ClientToSideChain clientToSideChain = (ClientToSideChain) CreateClient(chainUri, targetChainId, true);
            _clientsToSideChains.Add(targetChainId, clientToSideChain);
            return clientToSideChain;
        }

        /// <summary>
        /// Start a new client to the parent chain
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        private async Task CreateClientToParentChain()
        {
            if (!GrpcLocalConfig.Instance.Client)
                return;
            // do not use cache since configuration is managed by cluster
            var parent = GrpcRemoteConfig.Instance.ParentChain?.ElementAt(0);
            if (parent == null)
                throw new ChainInfoNotFoundException("Unable to get parent chain info.");
            _clientToParentChain = (ClientToParentChain) CreateClient(parent.Value.Value, parent.Value.Key, false);
            var height =
                await _chainManagerBasic.GetCurrentBlockHeightsync(ByteArrayHelpers.FromHexString(parent.Value.Key));
            _clientToParentChain.StartDuplexStreamingCall(_tokenSourceToParentChain.Token, height);
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="targetChainId"></param>
        /// <param name="toSideChain"> the client is connected to side chain or parent chain </param>
        /// <returns>
        /// return <see cref="ClientToParentChain"/>> if the client is to parent chain
        /// return <see cref="ClientToSideChain"/>> if the client is to side chain 
        /// </returns>    
        private ClientBase CreateClient(Uri uri, string targetChainId, bool toSideChain)
        {
            var uriStr = uri.ToString();
            var channel = CreateChannel(uriStr, targetChainId);
            if (toSideChain)
                return new ClientToSideChain(channel, _logger,
                    ByteArrayHelpers.FromHexString(targetChainId), _interval);
            return new ClientToParentChain(channel, _logger,
                ByteArrayHelpers.FromHexString(targetChainId), _interval);
        }

        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="CertificateException"></exception>
        private Channel CreateChannel(string uriStr, string targetChainId)
        {
            string crt = _certificateStore.GetCertificate(targetChainId);
            if (crt == null)
                throw new CertificateException("Unable to load Certificate.");
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            //var channel = new Channel(uriStr, ChannelCredentials.Insecure);
            return channel;
        }

        /// <summary>
        /// Take each side chain's header info 
        /// </summary>
        /// <returns>
        /// return the first one cached by every <see cref="ClientToSideChain"/> client
        /// </returns>
        public async Task<List<SideChainBlockInfo>> CollectSideChainBlockInfo()
        {
            List<SideChainBlockInfo> res = new List<SideChainBlockInfo>();
            foreach (var _ in _clientsToSideChains)
            {
                // take side chain info
                var targetHeight =
                    await _chainManagerBasic.GetCurrentBlockHeightsync(ByteArrayHelpers.FromHexString(_.Key));
                if (!_.Value.TryTake(Interval, out var blockInfo) || blockInfo.Height != targetHeight)
                    continue;

                res.Add((SideChainBlockInfo) blockInfo);
                await _chainManagerBasic.UpdateCurrentBlockHeightAsync(blockInfo.ChainId, blockInfo.Height + 1);
                await _chainManagerBasic.UpdateCurrentBlockHashAsync(blockInfo.ChainId,
                    ((SideChainBlockInfo) blockInfo).BlockHeaderHash);
            }

            return res;
        }

        /// <summary>
        /// Check the first cached one with <param name="blockInfo"></param> and remove it.
        /// </summary>
        /// <param name="blockInfo"></param>
        /// <returns>
        /// Return true and remove the first cached one as <param name="blockInfo"></param>.
        /// Return true if client for that chain is not existed which means the side chain is not available.
        /// Return false if it is not same or client for that chain is not existed.
        /// </returns>
        public bool TryRemoveSideChainBlockInfo(SideChainBlockInfo blockInfo)
        {
            if (!_clientsToSideChains.TryGetValue(blockInfo.ChainId.ToHex(), out var client))
                // TODO: this could be changed.
                return true;
            if (!client.First().Equals(blockInfo))
                return false;
            client.Take();
            return true;
        }
        
        /// <summary>
        /// Check the first cached one with <param name="blockInfo"></param> and remove it.
        /// </summary>
        /// <param name="blockInfo"></param>
        /// <returns>
        /// Return true and remove the first cached one as <param name="blockInfo"></param>.
        /// Return true if client for that chain is not existed which means the parent chain is not available.
        /// Return false if it is not same or client for that chain is not existed.
        /// </returns>
        public bool TryRemoveParentChainBlockInfo(ParentChainBlockInfo blockInfo)
        {
            if (_clientToParentChain == null)
                // TODO: this could be changed
                return true;
            if (!_clientToParentChain.First().Equals(blockInfo))
                return false;
            _clientToParentChain.Take();
            return true;
        }

        /// <summary>
        /// Try to take first one in cached queue
        /// </summary>
        /// <returns>
        /// return the first one cached by <see cref="ClientToParentChain"/>
        /// </returns>
        public async Task<ParentChainBlockInfo> CollectParentChainBlockInfo()
        {
            var chainId = GrpcRemoteConfig.Instance.ParentChain?.ElementAt(0).Key;
            if (chainId == null)
                return null;
            Hash parentChainId =
                ByteArrayHelpers.FromHexString(chainId);
            var targetHeight = await _chainManagerBasic.GetCurrentBlockHeightsync(parentChainId);
            if (_clientToParentChain.Empty() || _clientToParentChain.First().Height != targetHeight ||
                !_clientToParentChain.TryTake(Interval, out var blockInfo))
                return null;
            await _chainManagerBasic.UpdateCurrentBlockHeightAsync(blockInfo.ChainId, blockInfo.Height + 1);
            return (ParentChainBlockInfo) blockInfo;
        }

        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            _tokenSourceToSideChain?.Cancel();
            _tokenSourceToSideChain?.Dispose();

            //Todo: probably not needed
            _clientsToSideChains.Clear();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            _tokenSourceToParentChain?.Cancel();
            _tokenSourceToParentChain?.Dispose();

            //Todo: probably not needed
            _clientToParentChain = null;
        }
    }
}