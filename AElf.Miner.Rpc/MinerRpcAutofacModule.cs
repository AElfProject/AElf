using AElf.Configuration;
 using AElf.Miner.Rpc.Server;
 using Autofac;
 using AElf.Common;
using AElf.Configuration.Config.Chain;

namespace AElf.Miner.Rpc
 {
     public class MinerRpcAutofacModule : Module
     {
         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterType<SideChainBlockInfoRpcServer>().SingleInstance().OnActivated(impl =>
             {
                 impl.Instance.Init(Hash.LoadBase58(ChainConfig.Instance.ChainId));
             });
             builder.RegisterType<ParentChainBlockInfoRpcServer>().SingleInstance().OnActivated(impl =>
             {
                 impl.Instance.Init(Hash.LoadBase58(ChainConfig.Instance.ChainId));
             });
         }
     }
 }