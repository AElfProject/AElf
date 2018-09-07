using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
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
         private readonly Dictionary<string, MinerClient> _clients = new Dictionary<string, MinerClient>();

         private CertificateStore _certificateStore;
         private ILogger _logger;
         private IChainManagerBasic _chainManagerBasic;
         private Dictionary<string , Uri> ChildChains => GrpcRemoteConfig.Instance.ChildChains;
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
             _logger?.Debug(dir);
             _certificateStore = new CertificateStore(dir);
         }

         /// <summary>
         /// create multi client for different side chains
         /// this would be invoked when miner starts or configuration reloaded 
         /// </summary>
         /// <param name="token"></param>
         /// <returns></returns>
         public async Task CreateClientsToSideChain(CancellationToken token)
         {
             _clients.Clear();
             foreach (var sideChainId in ChildChains.Keys)
             {
                 var client = StartNewClientToSideChain(sideChainId);
                 var height =
                     await _chainManagerBasic.GetCurrentBlockHeightsync(ByteArrayHelpers.FromHexString(sideChainId));
                
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
         public MinerClient StartNewClientToSideChain(string targetChainId)
         {
             // NOTE: do not use cache if configuration is managed by cluster
             //if (_clients.TryGetValue(targetChainId, out var client)) return client;
             if(!ChildChains.TryGetValue(targetChainId, out var chainUri))
                 throw new ChainInfoNotFoundException($"Unable to get chain Info of {targetChainId}.");
             MinerClient client = CreateClient(chainUri, targetChainId);
             _clients.Add(targetChainId, client);
             return client;
         }
         
         /// <summary>
         /// start a new client to the parent chain
         /// </summary>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         /// <exception cref="ChainInfoNotFoundException"></exception>
         public MinerClient StartNewClientToParentChain(string targetChainId)
         {
             // do not use cache since configuration is managed by cluster
             if(!GrpcRemoteConfig.Instance.ParentChain.TryGetValue(targetChainId, out var chainUri))
                 throw new ChainInfoNotFoundException($"Unable to get chain Info of {targetChainId}.");
             return CreateClient(chainUri, targetChainId);
         }

         /// <summary>
         /// create a new client
         /// </summary>
         /// <param name="uri"></param>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         private MinerClient CreateClient(Uri uri, string targetChainId)
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
         private Channel CreateChannel(string uriStr, string targetChainId)
         {
             string crt = _certificateStore.GetCertificate(targetChainId);
             if(crt == null)
                 throw new CertificateException("Unable to load Certificate.");
             var channelCredentials = new SslCredentials(crt);
             //var channel = new Channel(uriStr, channelCredentials);
             var channel = new Channel(uriStr, ChannelCredentials.Insecure);
             return channel;
         }

         /// <summary>
         /// take each side chain's header info 
         /// </summary>
         /// <returns></returns>
         public async Task<List<SideChainIndexedInfo>> CollectSideChainIndexedInfo()
         {
             
             int interval = GrpcLocalConfig.Instance.WaitingIntervalInMillisecond;

             List<SideChainIndexedInfo> res = new List<SideChainIndexedInfo>();
             foreach (var _ in _clients)
             {
                 // take side chain info
                 var targetHeight = await _chainManagerBasic.GetCurrentBlockHeightsync(ByteArrayHelpers.FromHexString(_.Key));
                 if (!_.Value.TryTake(interval, out var responseSideChainIndexedInfo) || responseSideChainIndexedInfo.Height != targetHeight) 
                     continue;
                 System.Diagnostics.Debug.WriteLine("Got header info!");
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