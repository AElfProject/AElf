using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner.Rpc.Client
 {
     [LoggerName("MinerClient")]
     public class MinerClientManager
     {
         private readonly Dictionary<Hash, MinerClient> _clients = new Dictionary<Hash, MinerClient>();

         private CertificateStore _certificateStore;
         private ILogger _logger;
         private IChainManagerBasic _chainManagerBasic;
         public MinerClientManager(ILogger logger, IChainManagerBasic chainManagerBasic)
         {
             _logger = logger;
             _chainManagerBasic = chainManagerBasic;
             GrpcRemoteConfig.ConfigChanged+= GrpcRemoteConfigOnConfigChanged;
         }

         private void GrpcRemoteConfigOnConfigChanged(object sender, EventArgs e)
         {
             throw new NotImplementedException();
         }

         public void Init(string dir)
         {
             _certificateStore = new CertificateStore(dir);
         }

         /// <summary>
         /// create multi client for different side chains
         /// this would be invoked when miner is inited 
         /// </summary>
         /// <param name="token"></param>
         /// <returns></returns>
         public async Task CreateClientsToSideChain(CancellationToken token)
         {
             _clients.Clear();
             var sideChainIdList = await _chainManagerBasic.GetSideChainIdList();
             foreach (var sideChainId in sideChainIdList.ChainIds)
             {
                 var client = StartNewClientToSideChain(sideChainId);
                 var height = await _chainManagerBasic.GetCurrentBlockHeightsync(sideChainId);
                
                 // keep-alive
                 client.Index(token, height);
             }
         }
         
         /// <summary>
         /// start a new client to the side chain
         /// </summary>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         /// <exception cref="ChainInfoNotFoundException"></exception>
         public MinerClient StartNewClientToSideChain(Hash targetChainId)
         {
             // NOTE: do not use cache if configuration is managed by cluster
             //if (_clients.TryGetValue(targetChainId, out var client)) return client;
             string ch = targetChainId.ToHex();
             if(!GrpcRemoteConfig.Instance.ChildChains.TryGetValue(ch, out var chainUri))
                 throw new ChainInfoNotFoundException("Unable to get chain Info.");
             MinerClient client =  CreateClient(chainUri, targetChainId);
             _clients.Add(targetChainId, client);
             return client;
         }
         
         /// <summary>
         /// start a new client to the parent chain
         /// </summary>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         /// <exception cref="ChainInfoNotFoundException"></exception>
         public MinerClient StartNewClientToParentChain(Hash targetChainId)
         {
             // do not use cache since configuration is managed by cluster
             string ch = targetChainId.ToHex();
             if(!GrpcRemoteConfig.Instance.ParentChain.TryGetValue(ch, out var chainUri))
                 throw new ChainInfoNotFoundException("Unable to get chain Info.");
             return CreateClient(chainUri, targetChainId);
         }

         /// <summary>
         /// create a new client
         /// </summary>
         /// <param name="uri"></param>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         private MinerClient CreateClient(Uri uri, Hash targetChainId)
         {
             var uriStr = uri.Address + ":" + uri.Port;
             var channel = CreateChannel(uriStr, targetChainId);
             return new MinerClient(channel, _logger, targetChainId);
         }

         /// <summary>
         /// create a new channel
         /// </summary>
         /// <param name="uriStr"></param>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         /// <exception cref="CertificateException"></exception>
         private Channel CreateChannel(string uriStr, Hash targetChainId)
         {
             string ch = targetChainId.ToHex();
             string crt = _certificateStore.GetCertificate(ch);
             if(crt == null)
                 throw new CertificateException("Unable to load Certificate.");
             var channelCredentials = new SslCredentials(crt);
             var channel = new Channel(uriStr, channelCredentials);
             return channel;
         }

         /// <summary>
         /// take each side chain's header info 
         /// </summary>
         /// <returns></returns>
         public async Task<List<SideChainIndexedInfo>> CollectSideChainIndexedInfo(int interval)
         {
             List<SideChainIndexedInfo> res = new List<SideChainIndexedInfo>();
             foreach (var _ in _clients)
             {
                 // take side chain info
                 var targetHeight = await _chainManagerBasic.GetCurrentBlockHeightsync(_.Key);
                 if (_.Value.IndexedInfoQueue.Count == 0 || _.Value.IndexedInfoQueue.ElementAt(0).Height != targetHeight || _.Value.IndexedInfoQueue.TryTake(out var responseSideChainIndexedInfo, interval)) 
                     continue;
                 res.Add(new SideChainIndexedInfo
                 {
                     BlockHeaderHash = responseSideChainIndexedInfo.BlockHeaderHash,
                     ChainId = responseSideChainIndexedInfo.ChainId,
                     Height = responseSideChainIndexedInfo.Height,
                     TransactionMKRoot = responseSideChainIndexedInfo.TransactionMKRoot
                 });
                 await _chainManagerBasic.UpdateCurrentBlockHeightAsync(responseSideChainIndexedInfo.ChainId,
                     responseSideChainIndexedInfo.Height + 1);
             }
             return res;
         }
     }
 }