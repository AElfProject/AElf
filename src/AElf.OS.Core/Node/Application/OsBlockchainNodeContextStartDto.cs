using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;

namespace AElf.OS.Node.Application
{
    public class OsBlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }

        public List<GenesisSmartContractDto> InitializationSmartContracts { get; set; } =
            new List<GenesisSmartContractDto>();

        public Transaction[] InitializationTransactions { get; set; }

        public Type ZeroSmartContract { get; set; }

        public int SmartContractRunnerCategory { get; set; } = KernelConstants.DefaultRunnerCategory;
        
        public bool ContractDeploymentAuthorityRequired { get; set; }
    }
}