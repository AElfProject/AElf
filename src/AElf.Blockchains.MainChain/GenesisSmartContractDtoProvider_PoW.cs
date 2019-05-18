using System.Collections.Generic;
using AElf.Contracts.Consensus.PoW;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForPow(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract<PoWContract>(ConsensusSmartContractAddressNameProvider.Name,
                GeneratePoWInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GeneratePoWInitializationCallList()
        {
            var poWMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            poWMethodCallList.Add(nameof(PoWContract.InitialPoWContract),
                new SInt32Value
                {
                    Value = 2
                });
            return poWMethodCallList;
        }
    }
}