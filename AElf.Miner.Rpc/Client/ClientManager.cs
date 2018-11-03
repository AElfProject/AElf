using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using Google.Protobuf;
using Grpc.Core;
using NLog;
using NServiceKit.Common.Extensions;
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
        private int WaitingIntervalInMillisecond => GrpcLocalConfig.Instance.WaitingIntervalInMillisecond;

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
            _certificateStore = dir == "" ? _certificateStore : new CertificateStore(dir);
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();
            _interval = interval == 0 ? GlobalConfig.AElfInitCrossChainRequestInterval : interval;
            CreateClientsToSideChain().ConfigureAwait(false);
            CreateClientToParentChain().ConfigureAwait(false);
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval()
        {
            _clientToParentChain?.UpdateRequestInterval(GlobalConfig.AElfMiningInterval);
            _clientsToSideChains.AsParallel().ForEach(kv =>
            {
                kv.Value.UpdateRequestInterval(GlobalConfig.AElfMiningInterval);
            });
        }

        private async Task<ulong> GetTargetHeight(Hash chainId)
        {
            var height = await _chainManagerBasic.GetCurrentBlockHeightAsync(chainId);
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
                var height = await GetTargetHeight(Hash.LoadHex(sideChainId));

                // keep-alive
                client.StartDuplexStreamingCall(_tokenSourceToSideChain.Token, height).ConfigureAwait(false);
                _logger?.Info($"Created client to side chain {sideChainId}");
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
            try
            {
                if (!ChildChains.TryGetValue(targetChainId, out var chainUri))
                    throw new ChainInfoNotFoundException($"Unable to get chain Info of {targetChainId}.");
                ClientToSideChain clientToSideChain = (ClientToSideChain) CreateClient(chainUri, targetChainId, true);
                _clientsToSideChains.Add(Hash.LoadHex(targetChainId), clientToSideChain);
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
                var targetHeight = await GetTargetHeight(Hash.LoadHex(parent.ElementAt(0).Key)) ;
                _clientToParentChain.StartDuplexStreamingCall(_tokenSourceToParentChain.Token, targetHeight)
                    .ConfigureAwait(false);
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
                return new ClientToSideChain(channel, _logger, Hash.LoadHex(targetChainId), _interval, GlobalConfig.InvertibleChainHeight);
            return new ClientToParentChain(channel, _logger, Hash.LoadHex(targetChainId), _interval, GlobalConfig.InvertibleChainHeight);
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
                var targetHeight = await GetTargetHeight(_.Key);
                if (!_.Value.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo))
                    continue;
                
                res.Add((SideChainBlockInfo) blockInfo);
                await UpdateCrossChainInfo(blockInfo);
                _logger.Trace($"Removed side chain block info at height {blockInfo.Height}");
            }

            return res;
        }

        /// <summary>
        /// Update side chain information
        /// </summary>
        /// <param name="blockInfo"></param>
        /// <returns></returns>
        private async Task UpdateCrossChainInfo(IBlockInfo blockInfo)
        {
            await _chainManagerBasic.UpdateCurrentBlockHeightAsync(blockInfo.ChainId, blockInfo.Height);
        }

        public bool TryGetSideChainBlockInfo(SideChainBlockInfo scb)
        {
            if (scb == null)
                return false;
            if (!_clientsToSideChains.TryGetValue(scb.ChainId, out var client))
                // TODO: this could be changed.
                return true;
            var targetHeight = GetTargetHeight(scb.ChainId).Result;
            return client.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo) &&
                   scb.Equals(blockInfo);
        }
        
        /*/// <summary>
        /// Check the first cached one with <param name="blockInfo"></param> and remove it.
        /// </summary>
        /// <param name="blockInfo"></param>
        /// <returns>
        /// Return true and remove the first cached one as <param name="blockInfo"></param>.
        /// Return true if client for that chain is not existed which means the side chain is not available.
        /// Return false if it is not same to cached element.
        /// </returns>
        public async Task<bool> TryUpdateAndRemoveSideChainBlockInfo(SideChainBlockInfo blockInfo)
        {
            if (blockInfo == null)
                return true;
            
            if (!_clientsToSideChains.TryGetValue(blockInfo.ChainId, out var client))
            {
                await UpdateCrossChainInfo(blockInfo);
                return true;
            }
            if (client.Empty() || !client.First().Equals(blockInfo))
                return false;
            // TODO: this could be changed.
            var res = client.TryTake(WaitingIntervalInMillisecond * 2, blockInfo.Height, out var scb);
            if (!res || !scb.Equals(blockInfo))
            {
                // this should not happen in most cases
                //client.ReCacheBlockInfo(scb);
                return false;
            }
            await UpdateCrossChainInfo(blockInfo);
            _logger?.Info($"Removed side chain Info from {scb.ChainId} at height {scb.Height}");
            return true;
        }*/
        
        /*/// <summary>
        /// Check the first cached one with <param name="blockInfo"></param> and remove it.
        /// </summary>
        /// <param name="blockInfo"></param>
        /// <returns>
        /// Return true and remove the first cached one as <param name="blockInfo"></param>.
        /// Return true if client for that parent chain is not existed which means the parent chain is not available.
        /// Return false if it is not same or client for that chain is not existed.
        /// </returns>
        private bool TryRemoveParentChainBlockInfo(ParentChainBlockInfo blockInfo)
        {
            if (_clientToParentChain == null)
                return true;
            _logger?.Trace($"To remove parent chain info at height {blockInfo.Height}");

            if (_clientToParentChain.Empty() || !_clientToParentChain.First().Equals(blockInfo))
                return false;
            var res = _clientToParentChain.TryTake(WaitingIntervalInMillisecond * 2, blockInfo.Height, out var pcb);

            if (!res || !pcb.Equals(blockInfo))
            {
                // this should not happen in most cases
                //_clientToParentChain.ReCacheBlockInfo(pcb);
                return false;
            }
            _logger?.Trace($"Removed parent chain info at height {pcb.Height}");
            return true;
        }*/

        /// <summary>
        /// Try to take first one in cached queue
        /// </summary>
        /// <returns>
        /// return the first one cached by <see cref="ClientToParentChain"/>
        /// </returns>
        public async Task<ParentChainBlockInfo> TryGetParentChainBlockInfo(ParentChainBlockInfo pcb = null)
        {
            if (!GrpcLocalConfig.Instance.ClientToParentChain)
                throw new ClientShutDownException("Client to parent chain is shut down");
            if (_clientToParentChain == null)
                return null;
            var chainId = GrpcRemoteConfig.Instance.ParentChain?.ElementAtOrDefault(0).Key;
            if (chainId == null)
                return null;
            Hash parentChainId = Hash.LoadHex(chainId);
            ulong targetHeight = await GetTargetHeight(parentChainId);
            if (pcb != null && !pcb.ChainId.Equals(parentChainId) &&targetHeight != pcb.Height)
                return null;

            if (!_clientToParentChain.TryTake(WaitingIntervalInMillisecond, targetHeight, out var blockInfo))
            {
                return null;
            }

            if (pcb == null || pcb.Equals(blockInfo))
            {
                return (ParentChainBlockInfo) blockInfo;
            }
            
            _logger.Trace($"Cached parent block info is {blockInfo}");
            _logger.Trace($"Parent block info in transaction is {pcb}");
            return null;
        }
        
        /*/// <summary>
        /// Update parent chain block cached in client and database.
        /// </summary>
        /// <param name="parentChainBlockInfo"></param>
        /// <returns></returns>
        public async Task<bool> UpdateParentChainBlockInfo(ParentChainBlockInfo parentChainBlockInfo)
        {
            if (parentChainBlockInfo == null)
                // TODO: this could be changed
                return true;
            if (!TryRemoveParentChainBlockInfo(parentChainBlockInfo))
                return false;
            await UpdateCrossChainInfo(parentChainBlockInfo);
            return true;
        }*/
        
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