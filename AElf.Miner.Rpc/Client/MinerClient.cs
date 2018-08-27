using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.GRPC;
using AElf.Kernel;
using Akka.Util.Internal;
using Grpc.Core;
 
 namespace AElf.Miner.Rpc.Client
 {
     public class MinerClient
     {
         private readonly Dictionary<string, HeaderInfoRpc.HeaderInfoRpcClient> _channels = new Dictionary<string, HeaderInfoRpc.HeaderInfoRpcClient>();
         public void Init()
         {
             var childChains = GrpcConfig.Instance.ChildChains;
             foreach (var chainIdUri in childChains)
             {
                 var uri = chainIdUri.Value.Address + ":" + chainIdUri.Value.Port;
                 var channOptions = new List<ChannelOption>
                 {
                     new ChannelOption(ChannelOptions.SslTargetNameOverride,"aelf")
                 };
                // var channelCredentials = new SslCredentials(File.ReadAllText(Path.Combine(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + chainIdUri.Key + "_cert.pem")));
                 string certificate = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "sidechain_cert.pem");
                 string privateKey = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "sidechain_key.pem");
                 string crt = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "main_cert.pem");
                 var channelCredentials = new SslCredentials(crt);
                 var channel = new Channel(uri, channelCredentials);
                 _channels.Add(chainIdUri.Key, new HeaderInfoRpc.HeaderInfoRpcClient(channel));
             }
         }

         public ReponseIndexedInfo GetHeaderInfo(RequestIndexedInfo request)
         {
             if (!_channels.TryGetValue(request.ChainId.ToHex(), out var client))
             {
                 throw new Exception("Not existed chain");
             }
             var headerInfo = client.GetHeaderInfo(request);
             return headerInfo;
         }
     }
 }