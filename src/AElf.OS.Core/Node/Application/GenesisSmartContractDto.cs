using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.OS.Node.Application
{
    public class GenesisSmartContractDto
    {
        public byte[] Code { get; set; }
        public Hash SystemSmartContractName { get; set; }

        public List<ContractInitializationMethodCall> ContractInitializationMethodCallList { get; set; }
    }
}