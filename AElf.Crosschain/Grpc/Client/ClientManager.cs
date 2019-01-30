using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.Common;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.GRPC;
using AElf.Crosschain.Exceptions;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Crosschain.Grpc.Client
{
    public class ClientManager
    {
        private readonly Dictionary<int, GrpcSideChainBlockInfoRpcClient> _clientsToSideChains =
            new Dictionary<int, GrpcSideChainBlockInfoRpcClient>();

        private GrpcParentChainBlockInfoRpcClient _grpcParentChainBlockInfoRpcClient;
        private readonly ICrossChainInfoReader _crossChainInfoReader;
        private CertificateStore _certificateStore;
        public ILogger<ClientManager> Logger {get;set;}
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;
        private int _interval;

        /// <summary>
        /// Waiting interval for taking element.
        /// </summary>
        private int WaitingIntervalInMillisecond => GrpcLocalConfig.Instance.WaitingIntervalInMillisecond;

        public ClientManager( ICrossChainInfoReader crossChainInfoReader)
        {
            Logger = NullLogger<ClientManager>.Instance;
            _crossChainInfoReader = crossChainInfoReader;
            //GrpcRemoteConfig.ConfigChanged += GrpcRemoteConfigOnConfigChanged;
        }

        /*private void GrpcRemoteConfigOnConfigChanged(object sender, EventArgs e)
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
        }*/

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
            
            // todo : subscribe event for client management
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval()
        {
            _grpcParentChainBlockInfoRpcClient?.UpdateRequestInterval(ConsensusConfig.Instance.DPoSMiningInterval);
            _clientsToSideChains.AsParallel().ToList().ForEach(kv =>
            {
                kv.Value.UpdateRequestInterval(ConsensusConfig.Instance.DPoSMiningInterval);
            });
        }

        /*private async Task<ulong> GetSideChainTargetHeight(int chainId)
        {
            var height = await _crossChainInfoReader.GetSideChainCurrentHeightAsync(chainId);
            return height == 0 ? GlobalConfig.GenesisBlockHeight : height + 1;
        }
        
        private async Task<ulong> GetParentChainTargetHeight()
        {
            var height = await _crossChainInfoReader.GetParentChainCurrentHeightAsync();
            return height == 0 ? GlobalConfig.GenesisBlockHeight : height + 1;
        }*/
        
        #region Create client

        public void CreateClient(ClientBase clientCache)
        {
            var client = CreateGrpcClient(clientCache);
            //client = clientBasicInfo.TargetIsSideChain ? (ClientToSideChain) client : (ClientToParentChain) client;
            client.StartDuplexStreamingCall(clientCache.TargetIsSideChain
                ? _tokenSourceToSideChain.Token
                : _tokenSourceToParentChain.Token);
        }
        
        /// <summary>
        /// Start a new client to the side chain
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        /*private ClientToSideChain CreateClientToSideChain(string uriStr, int targetChainId)
        {
            // NOTE: do not use cache if configuration is managed by cluster
            //if (_clientsToSideChains.TryGetValue(targetChainId, out var clientToSideChain)) return clientToSideChain;
            try
            {
                ClientToSideChain clientToSideChain = (ClientToSideChain) CreateGrpcClient(uriStr, targetChainId, true);
                _clientsToSideChains.Add(targetChainId, clientToSideChain);
                return clientToSideChain;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while creating client to side chain.");
                throw;
            }
        }*/

        
        /// <summary>
        /// Start a new client to the parent chain
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ChainInfoNotFoundException"></exception>
        /*private async Task CreateClientToParentChain()
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
                    (ClientToParentChain) CreateGrpcClient(parent.ElementAt(0).Value, parent.ElementAt(0).Key, false);
                var targetHeight = await GetParentChainTargetHeight();
                // TODO: maybe improvement for NO wait call
                var task = _clientToParentChain.StartDuplexStreamingCall(_tokenSourceToParentChain.Token, targetHeight);
                Logger.LogInformation($"Created client to parent chain {parent.ElementAt(0).Key}");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while create client to parent chain.");
                throw;
            }
        }*/

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// return <see cref="GrpcParentChainBlockInfoRpcClient"/>> Client is to parent chain if <param name="isClientToSideChain"/> is false.
        /// return <see cref="GrpcSideChainBlockInfoRpcClient"/>> Client is to side chain if <param name="isClientToSideChain"/> is true. 
        /// </returns>    
        private IGrpcCrossChainClient CreateGrpcClient(ClientBase clientCache)
        {
            var channel = CreateChannel(clientCache.ToUriStr(), clientCache.TargetChainId);

            if (clientCache.TargetIsSideChain)
            {
                var clientToSideChain = new GrpcSideChainBlockInfoRpcClient(channel, clientCache);
                return clientToSideChain;
            }
            _grpcParentChainBlockInfoRpcClient = new GrpcParentChainBlockInfoRpcClient(channel, clientCache);
            return _grpcParentChainBlockInfoRpcClient;
        }

        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="CertificateException"></exception>
        private Channel CreateChannel(string uriStr, int targetChainId)
        {
            string crt = _certificateStore.GetCertificate(targetChainId.ToString());
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
        /// return the first one cached by every <see cref="GrpcSideChainBlockInfoRpcClient"/> client
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
                if (!_.Value.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo, needToCheckCachingCount: true))
                    continue;
                
                res.Add((SideChainBlockInfo) blockInfo);
                Logger.LogTrace($"Removed side chain block info at height {blockInfo.Height}");
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
        /// return the first one cached by <see cref="GrpcParentChainBlockInfoRpcClient"/>
        /// </returns>
        public async Task<List<ParentChainBlockInfo>> TryGetParentChainBlockInfo(ParentChainBlockInfo[] parentChainBlocks = null)
        {
            if (!GrpcLocalConfig.Instance.ClientToParentChain)
                throw new ClientShutDownException("Client to parent chain is shut down");
            if (_grpcParentChainBlockInfoRpcClient == null)
                return null;
            var chainId = GrpcRemoteConfig.Instance.ParentChain?.ElementAtOrDefault(0).Key;
            if (chainId == null)
                return null;
            var parentChainId = chainId.ConvertBase58ToChainId();
            ulong targetHeight = await GetParentChainTargetHeight();

            List<ParentChainBlockInfo> parentChainBlockInfos = new List<ParentChainBlockInfo>();
            var isMining = parentChainBlocks == null;
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (!isMining && parentChainBlocks.Length > GlobalConfig.MaximalCountForIndexingParentChainBlock)
                return null;
            int length = parentChainBlocks?.Length ?? GlobalConfig.MaximalCountForIndexingParentChainBlock;
            
            int i = 0;
            while (i < length)
            {
                var pcb = parentChainBlocks?[i];
                if (!isMining && (pcb == null || !pcb.ChainId.Equals(parentChainId) || targetHeight != pcb.Height))
                    return null;

                if (!_grpcParentChainBlockInfoRpcClient.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo, isMining))
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
                i++;
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
            _grpcParentChainBlockInfoRpcClient = null;
        }
    }
}