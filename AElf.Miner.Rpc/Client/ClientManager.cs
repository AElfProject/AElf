using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using Google.Protobuf;
using Grpc.Core;
using NLog;
using ClientBase = AElf.Miner.Rpc.Client.ClientBase;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner.Rpc.Client
{
    [LoggerName("MinerClient")]
    public class ClientManager
    {
        private readonly Dictionary<Hash, ClientToSideChain> _clientsToSideChains =
            new Dictionary<Hash, ClientToSideChain>();

        private ClientToParentChain _clientToParentChain;
        private readonly ICrossChainInfoReader _crossChainInfoReader;
        private CertificateStore _certificateStore;
        private readonly ILogger _logger;
        private Dictionary<string, Uri> ChildChains => GrpcRemoteConfig.Instance.ChildChains;
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;
        private int _interval;

        /// <summary>
        /// Waiting interval for taking element.
        /// </summary>
        private int WaitingIntervalInMillisecond => GrpcLocalConfig.Instance.WaitingIntervalInMillisecond;

        public ClientManager(ILogger logger, ICrossChainInfoReader crossChainInfoReader)
        {
            _logger = logger;
            _crossChainInfoReader = crossChainInfoReader;
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
            _certificateStore = dir == "" ? _certificateStore : new CertificateStore(dir);
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();
            _interval = interval == 0 ? GlobalConfig.AElfInitCrossChainRequestInterval : interval;
            CreateClientsToSideChain().Wait();
            CreateClientToParentChain().Wait();
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval()
        {
            _clientToParentChain?.UpdateRequestInterval(ConsensusConfig.Instance.DPoSMiningInterval);
            _clientsToSideChains.AsParallel().ToList().ForEach(kv =>
            {
                kv.Value.UpdateRequestInterval(ConsensusConfig.Instance.DPoSMiningInterval);
            });
        }

        private async Task<ulong> GetSideChainTargetHeight(Hash chainId)
        {
            var height = await _crossChainInfoReader.GetSideChainCurrentHeightAsync(chainId);
            return height == 0 ? GlobalConfig.GenesisBlockHeight : height + 1;
        }
        
        private async Task<ulong> GetParentChainTargetHeight()
        {
            var height = await _crossChainInfoReader.GetParentChainCurrentHeightAsync();
            return height == 0 ? GlobalConfig.GenesisBlockHeight : height + 1;
        }
        
        #region Create client
        
        /// <summary>
        /// Create multi clients for different side chains
        /// this would be invoked when miner starts or configuration reloaded 
        /// </summary>
        /// <returns></returns>
        private async Task CreateClientsToSideChain()
        {
            if (!GrpcLocalConfig.Instance.ClientToSideChain)
                return;

            _clientsToSideChains.Clear();
            foreach (var sideChainId in ChildChains.Keys)
            {
                var client = CreateClientToSideChain(sideChainId);
                var height = await GetSideChainTargetHeight(Hash.LoadBase58(sideChainId));

                // keep-alive
                // TODO: maybe improvement for NO wait call 
                var task = client.StartDuplexStreamingCall(_tokenSourceToSideChain.Token, height);
                _logger?.Info($"Created client to side chain {sideChainId}");
            }
        }


        /// <summary>
        /// Start a new client to the side chain
        /// </summary>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        private ClientToSideChain CreateClientToSideChain(string targetChainId)
        {
            // NOTE: do not use cache if configuration is managed by cluster
            //if (_clientsToSideChains.TryGetValue(targetChainId, out var clientToSideChain)) return clientToSideChain;
            try
            {
                if (!ChildChains.TryGetValue(targetChainId, out var chainUri))
                    throw new ChainInfoNotFoundException($"Unable to get chain Info of {targetChainId}.");
                ClientToSideChain clientToSideChain = (ClientToSideChain) CreateClient(chainUri, targetChainId, true);
                _clientsToSideChains.Add(Hash.LoadBase58(targetChainId), clientToSideChain);
                return clientToSideChain;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while creating client to side chain.");
                throw;
            }
        }

        
        /// <summary>
        /// Start a new client to the parent chain
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        private async Task CreateClientToParentChain()
        {
            if (!GrpcLocalConfig.Instance.ClientToParentChain)
                return;
            try
            {
                // do not use cache since configuration is managed by cluster
                var parent = GrpcRemoteConfig.Instance.ParentChain;
                if (parent == null || parent.Count == 0)
                    throw new ChainInfoNotFoundException("Unable to get parent chain info.");
                _clientToParentChain =
                    (ClientToParentChain) CreateClient(parent.ElementAt(0).Value, parent.ElementAt(0).Key, false);
                var targetHeight = await GetParentChainTargetHeight();
                // TODO: maybe improvement for NO wait call
                var task = _clientToParentChain.StartDuplexStreamingCall(_tokenSourceToParentChain.Token, targetHeight);
                _logger?.Info($"Created client to parent chain {parent.ElementAt(0).Key}");
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while create client to parent chain.");
                throw;
            }
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="targetChainId"></param>
        /// <param name="isClientToSideChain"> the client is connected to side chain or parent chain </param>
        /// <returns>
        /// return <see cref="ClientToParentChain"/>> if the client is to parent chain
        /// return <see cref="ClientToSideChain"/>> if the client is to side chain 
        /// </returns>    
        private ClientBase CreateClient(Uri uri, string targetChainId, bool isClientToSideChain)
        {
            var uriStr = uri.ToString();
            var channel = CreateChannel(uriStr, targetChainId);
            var chainId = Hash.LoadBase58(targetChainId);
            if (isClientToSideChain)
                return new ClientToSideChain(channel, _logger, chainId, _interval,  GlobalConfig.InvertibleChainHeight, GlobalConfig.MaximalCountForIndexingSideChainBlock);
            return new ClientToParentChain(channel, _logger, chainId, _interval, 
                GlobalConfig.InvertibleChainHeight,  GlobalConfig.MaximalCountForIndexingParentChainBlock);
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
            return channel;
        }

        #endregion

        
        #region Chain block info process

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
                // index only one block from one side chain.
                // this could be changed later.
                var targetHeight = await GetSideChainTargetHeight(_.Key);
                if (!_.Value.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo, cachingThreshold: true))
                    continue;
                
                res.Add((SideChainBlockInfo) blockInfo);
                _logger.Trace($"Removed side chain block info at height {blockInfo.Height}");
            }

            return res;
        }

        public async Task<bool> TryGetSideChainBlockInfo(SideChainBlockInfo scb)
        {
            if (scb == null)
                return false;
            if (!_clientsToSideChains.TryGetValue(scb.ChainId, out var client))
                // TODO: this could be changed.
                return true;
            var targetHeight = await GetSideChainTargetHeight(scb.ChainId);
            return client.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo) &&
                   scb.Equals(blockInfo);
        }
        
        /// <summary>
        /// Try to take first one in cached queue
        /// </summary>
        /// <param name="parentChainBlocks"> Mining processing if it is null, otherwise synchronization processing.</param>
        /// <returns>
        /// return the first one cached by <see cref="ClientToParentChain"/>
        /// </returns>
        public async Task<List<ParentChainBlockInfo>> TryGetParentChainBlockInfo(ParentChainBlockInfo[] parentChainBlocks = null)
        {
            if (!GrpcLocalConfig.Instance.ClientToParentChain)
                throw new ClientShutDownException("Client to parent chain is shut down");
            if (_clientToParentChain == null)
                return null;
            var chainId = GrpcRemoteConfig.Instance.ParentChain?.ElementAtOrDefault(0).Key;
            if (chainId == null)
                return null;
            Hash parentChainId = Hash.LoadBase58(chainId);
            ulong targetHeight = await GetParentChainTargetHeight();

            List<ParentChainBlockInfo> parentChainBlockInfos = new List<ParentChainBlockInfo>();
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            int length = parentChainBlocks?.Length ?? GlobalConfig.MaximalCountForIndexingParentChainBlock;
            int i = 0;
            while (i++ < length)
            {
                var pcb = parentChainBlocks?[i];
                var isMining = pcb == null;
                if (!isMining && (!pcb.ChainId.Equals(parentChainId) || targetHeight != pcb.Height))
                    return null;

                if (!_clientToParentChain.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo, isMining))
                {
                    // no more available parent chain block info
                    parentChainBlockInfos = isMining && parentChainBlockInfos.Count > 0 ? parentChainBlockInfos : null;
                    break;
                }
                
                if (!isMining && !pcb.Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return null;
                parentChainBlockInfos.Add((ParentChainBlockInfo) blockInfo);
                targetHeight++;
            }
            
            return parentChainBlockInfos;
        }
        #endregion


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