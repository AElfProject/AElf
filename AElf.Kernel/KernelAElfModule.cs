using System;
using AElf.Common;
using AElf.Common.Enums;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.Network;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAElfModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.AElfDPoS)
            {
                GlobalConfig.AElfDPoSMiningInterval = ConsensusConfig.Instance.DPoSMiningInterval;
                if (NodeConfig.Instance.ConsensusInfoGenerater)
                {
                    Console.WriteLine($"Mining interval: {GlobalConfig.AElfDPoSMiningInterval} ms");
                }
            }

            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.PoTC)
            {
                GlobalConfig.BlockProducerNumber = 1;
                GlobalConfig.ExpectedTransactionCount = ConsensusConfig.Instance.ExpectedTransactionCount;
            }

            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.SingleNode)
            {
                GlobalConfig.BlockProducerNumber = 1;
                GlobalConfig.SingleNodeTestMiningInterval = ConsensusConfig.Instance.SingleNodeTestMiningInterval;
                Console.WriteLine($"Mining interval: {GlobalConfig.SingleNodeTestMiningInterval} ms");
            }
            
            builder.RegisterModule(new KernelAutofacModule());
            builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }

        public void Run(ILifetimeScope scope)
        {

        }
    }
}