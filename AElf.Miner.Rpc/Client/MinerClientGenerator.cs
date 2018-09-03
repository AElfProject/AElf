using System;
using System.Collections.Generic;
using AElf.Common.Application;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Miner.Rpc.Client
 {
     public class MinerClientGenerator
     {
         private readonly Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient> _clients =
             new Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient>();

         private CertificateStore _certificateStore;

         /*public ReponseIndexedInfo GetHeaderInfo(Hash chainId, RequestIndexedInfo request)
         {
             if (!_clients.TryGetValue(chainId, out var client))
             {
                 throw new ClientNotFoundException("Not existed client");
             }
             var headerInfo = client.GetHeaderInfo(request);
             return headerInfo;
         }*/

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
             return CreateClient(chainUri, ch);
         }
         
         /// <summary>
         /// start a new client to the parent chain
         /// </summary>
         /// <param name="targetChainId"></param>
         /// <returns></returns>
         /// <exception cref="ChainInfoNotFoundException"></exception>
         public MinerClient StartNewClientToParentChain(Hash targetChainId)
         {
             // do not use cache if configuration is managed by cluster
             //if (_clients.TryGetValue(targetChainId, out var client)) return client;
             string ch = targetChainId.ToHex();
             if(!GrpcRemoteConfig.Instance.ParentChain.TryGetValue(ch, out var chainUri))
                 throw new ChainInfoNotFoundException("Unable to get chain Info.");
             return CreateClient(chainUri, ch);
         }

         /// <summary>
         /// create a new client
         /// </summary>
         /// <param name="uri"></param>
         /// <param name="chainId"></param>
         /// <returns></returns>
         private MinerClient CreateClient(Uri uri, string chainId)
         {
             var uriStr = uri.Address + ":" + uri.Port;
             var channel = CreateChannel(uriStr, chainId);
             return new MinerClient(channel);
         }

         /// <summary>
         /// create a new channel
         /// </summary>
         /// <param name="uriStr"></param>
         /// <param name="chainId"></param>
         /// <returns></returns>
         /// <exception cref="CertificateException"></exception>
         private Channel CreateChannel(string uriStr, string chainId)
         {
             string crt = _certificateStore.GetCertificate(chainId);
             if(crt == null)
                 throw new CertificateException("Unable to load Certificate.");
             var channelCredentials = new SslCredentials(crt);
             var channel = new Channel(uriStr, channelCredentials);
             return channel;
         }
     }
 }