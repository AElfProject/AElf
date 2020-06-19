using System.Collections.Generic;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class AEDPoSContractInitializationDataProvider: IAEDPoSContractInitializationDataProvider
    {
        public AEDPoSContractInitializationData GetContractInitializationData()
        {
            return new AEDPoSContractInitializationData
            {
                MiningInterval = 4000,
                PeriodSeconds = 604800,
                StartTimestamp = new Timestamp {Seconds = 0},
                InitialMinerList = new List<string> {SampleAccount.Accounts[0].KeyPair.PublicKey.ToHex()},
                MinerIncreaseInterval = 31536000
            };
        }
    }
}