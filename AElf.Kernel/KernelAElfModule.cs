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
                Globals.AElfDPoSMiningInterval = ConsensusConfig.Instance.DPoSMiningInterval;
                if (NodeConfig.Instance.ConsensusInfoGenerater)
                {
                    Console.WriteLine($"Mining interval: {Globals.AElfDPoSMiningInterval} ms");
                }
            }

            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.PoTC)
            {
                Globals.BlockProducerNumber = 1;
                Globals.ExpectedTransactionCount = ConsensusConfig.Instance.ExpectedTransanctionCount;
            }

            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.SingleNode)
            {
                Globals.BlockProducerNumber = 1;
                Globals.SingleNodeTestMiningInterval = ConsensusConfig.Instance.SingleNodeTestMiningInterval;
                Console.WriteLine($"Mining interval: {Globals.SingleNodeTestMiningInterval} ms");
            }
            
            builder.RegisterModule(new KernelAutofacModule());
            builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }

        public void Run(ILifetimeScope scope)
        {

        }
    }
}