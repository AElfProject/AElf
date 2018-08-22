using System;
using System.Collections.Generic;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.GRPC;
using AElf.Kernel;
using Akka.Util.Internal;
using Grpc.Core;
 
 namespace AElf.Miner.Rpc.Client
 {
     public class MinerClient
     {
         private readonly Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient> _channels = new Dictionary<Hash, HeaderInfoRpc.HeaderInfoRpcClient>();
         public void Init()
         {
             var childChains = GrpcConfig.Instance.ChildChains;
             foreach (var chainIdUri in childChains)
             {
                 var uri = chainIdUri.Value.Address + ":" + chainIdUri.Value.Port;
                 _channels.Add(ByteArrayHelpers.FromHexString(chainIdUri.Key),
                     new HeaderInfoRpc.HeaderInfoRpcClient(new Channel(uri, ChannelCredentials.Insecure)));
             }
             /*var channelCredentials = new SslCredentials(File.ReadAllText("roots.pem"));  // Load a custom roots file.
             var channel = new Channel("myservice.example.com", channelCredentials);*/
         }

         public ReponseIndexedInfo GetHeaderInfo(RequestIndexedInfo request)
         {
             if (!_channels.TryGetValue(request.ChainId, out var client))
             {
                 throw new Exception("Not existed chain");
             }

             var headerInfo = client.GetHeaderInfo(request);
             return headerInfo;
         }
     }
 }