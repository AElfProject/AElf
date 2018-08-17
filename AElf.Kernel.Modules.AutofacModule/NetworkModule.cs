using AElf.Configuration.Config.Network;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Autofac;
using Community.AspNetCore.JsonRpc;

namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public bool IsMiner { get; }

         public NetworkModule(bool isMiner)
         {
             IsMiner = isMiner;
         }

         protected override void Load(ContainerBuilder builder)
         {             
             /*if(IsMiner)
                 builder.RegisterType<BootnodePeerManager>().As<IPeerManager>();
             else
                 builder.RegisterType<PeerManager>().As<IPeerManager>();*/

             builder.RegisterType<PeerManager>().As<IPeerManager>().SingleInstance();
             //builder.RegisterType<PeerManager>().As<IPeerManager>().As<IJsonRpcService>();
             
             builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
             builder.RegisterType<ConnectionListener>().As<IConnectionListener>();
                 
             PeerDataStore peerDb = new PeerDataStore(NetworkConfig.Instance.PeersDbPath);
             builder.RegisterInstance(peerDb).As<IPeerDatabase>();

             //NodeData nd = NodeData.FromString(NetConfig.Host + ":" + NetConfig.Port);
             
             //NodeDialer dialer = new NodeDialer(NetConfig.Port);
             //builder.RegisterInstance(dialer).As<INodeDialer>();

         }
     }
 }