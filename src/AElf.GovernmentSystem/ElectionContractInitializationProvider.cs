using System.Collections.Generic;
using AElf.Contracts.Election;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class ElectionContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        private readonly EconomicOptions _economicOptions;
        private readonly AEDPoSOptions _aeDPoSOptions;

        public Hash SystemSmartContractName { get; } = ElectionSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Election";


        public ElectionContractInitializationProvider(
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<AEDPoSOptions> aeDPoSOptions)
        {
            _aeDPoSOptions = aeDPoSOptions.Value;
            _economicOptions = economicOptions.Value;
        }

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall{
                    MethodName = nameof(ElectionContractContainer.ElectionContractStub.InitialElectionContract),
                    Params = new InitialElectionContractInput
                    {
                        MaximumLockTime = _economicOptions.MaximumLockTime,
                        MinimumLockTime = _economicOptions.MinimumLockTime,
                        TimeEachTerm = _aeDPoSOptions.PeriodSeconds,
                        MinerList = {_aeDPoSOptions.InitialMinerList},
                        MinerIncreaseInterval = _aeDPoSOptions.MinerIncreaseInterval
                    }.ToByteString()
                }
            };
        }
    }
}