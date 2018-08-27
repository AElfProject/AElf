using System;
using System.Collections.Generic;
using AElf.Common.Application;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
 
 namespace AElf.Miner.Rpc.Client
 {
     public class MinerClient
     {
         private readonly Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient> _clients =
             new Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient>();

         private CertificateStore _certificateStore;

         public ReponseIndexedInfo GetHeaderInfo(Hash chainId, RequestIndexedInfo request)
         {
             if (!_clients.TryGetValue(chainId, out var client))
             {
                 throw new ClientNotFoundException("Not existed client");
             }
             var headerInfo = client.GetHeaderInfo(request);
             return headerInfo;
         }

         public void Init(string dir)
         {
             _certificateStore =
                 new CertificateStore(dir);
         }
         
         public HeaderInfoRpc.HeaderInfoRpcClient StartNewClient(Hash targetChainId)
         {
             if (_clients.TryGetValue(targetChainId, out var client)) return client;
             string ch = targetChainId.ToHex();
             if(!GrpcConfig.Instance.ChildChains.ContainsKey(ch))
                 throw new ChainInfoNotFoundException("Unable to get chain Info.");
             var chainIdUri = GrpcConfig.Instance.ChildChains[ch];
             var uri = chainIdUri.Address + ":" + chainIdUri.Port;
             string crt = _certificateStore.GetCertificate(ch);
             if(crt == null)
                 throw new CertificateException("Unable to load Certificate.");
             var channelCredentials = new SslCredentials(crt);
             var channel = new Channel(uri, channelCredentials);
             client = new HeaderInfoRpc.HeaderInfoRpcClient(channel);
             _clients.Add(targetChainId, client);
             return client;
         }
     }
 }