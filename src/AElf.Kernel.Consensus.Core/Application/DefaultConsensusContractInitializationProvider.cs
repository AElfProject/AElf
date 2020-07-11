using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.Application
{
    public class DefaultConsensusContractInitializationProvider : IContractInitializationProvider
    {
        private readonly ConsensusOptions _consensusOptions;

        public DefaultConsensusContractInitializationProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
        }

        public Hash SystemSmartContractName => ConsensusSmartContractAddressNameProvider.Name;
        public string ContractCodeName => _consensusOptions.ConsensusContractCodeName;

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}