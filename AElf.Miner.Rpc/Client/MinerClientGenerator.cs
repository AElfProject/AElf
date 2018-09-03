using System;
using System.Collections.Generic;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner.Rpc.Client
 {
     [LoggerName("MinerClient")]
     public class MinerClientGenerator
     {
         private readonly Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient> _clients =
             new Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient>();

         private CertificateStore _certificateStore;
         private ILogger _logger;

         public MinerClientGenerator(ILogger logger)
         {
             _logger = logger;
         }

         public void Init(string dir)
         {
             _certificateStore = new CertificateStore(dir);
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
             return CreateClient(chainUri, targetChainId);
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
     }
 }