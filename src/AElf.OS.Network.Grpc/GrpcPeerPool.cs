//using System;
//using AElf.Kernel.Account.Application;
//using AElf.OS.Network.Domain;
//using AElf.OS.Network.Infrastructure;
//using Microsoft.Extensions.Logging;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Threading;
//
//namespace AElf.OS.Network.Grpc
//{
//    // TODO: Extract into a generic base class in OS.Core
//    public class GrpcPeerPool : PeerPool<GrpcPeer>, ISingletonDependency
//    {
//        private readonly IAccountService _accountService;
//        private readonly INodeManager _nodeManager;
//
//        public GrpcPeerPool(IAccountService accountService, INodeManager nodeManager)
//        {
//            _accountService = accountService;
//            _nodeManager = nodeManager;
//        }
//        
//        public override bool TryAddPeer(GrpcPeer p)
//        {
//            string localPubKey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();
//
//            if (p.Info.Pubkey == localPubKey)
//                throw new InvalidOperationException($"Connection to self detected {p.Info.Pubkey} ({p.IpAddress})");
//
//            if (!AuthenticatedPeers.TryAdd(p.Info.Pubkey, p))
//            {
//                Logger.LogWarning($"Could not add peer {p.Info.Pubkey} ({p.IpAddress})");
//                return false;
//            }
//            
//            AsyncHelper.RunSync(() => _nodeManager.AddNodeAsync(new Node { Pubkey = p.Info.Pubkey.ToByteString(), Endpoint = p.IpAddress}));
//            
//            return true;
//        }
//
//        public GrpcPeer GetGrpcPeer(string pubkey)
//        {
//            AuthenticatedPeers.TryGetValue(pubkey, out GrpcPeer peer);
//            return peer;
//        }
//    }
//}