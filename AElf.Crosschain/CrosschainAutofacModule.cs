using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Crosschain.Server;
using Autofac;

namespace AElf.Crosschain
 {
     public class CrosschainAutofacModule : Module
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