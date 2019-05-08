using System.Collections.Generic;
using AElf.Contracts.Consensus.MinersCountProvider;
using AElf.Contracts.Vote;
using AElf.Kernel;
using AElf.Kernel.Consensus.AElfConsensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Vote;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForMinersCountProvider(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract<MinersCountProviderContract>(
                MinersCountProviderSmartContractAddress.Name, GenerateMinersCountProviderInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateMinersCountProviderInitializationCallList()
        {
            var minersCountProviderContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            return minersCountProviderContractMethodCallList;
        }
    }
}