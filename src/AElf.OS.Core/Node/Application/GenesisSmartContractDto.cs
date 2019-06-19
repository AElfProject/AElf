using Acs0;
using AElf.Types;

namespace AElf.OS.Node.Application
{
    public class GenesisSmartContractDto
    {
        public byte[] Code { get; set; }
        public Hash SystemSmartContractName { get; set; }

        public SystemContractDeploymentInput.Types.SystemTransactionMethodCallList TransactionMethodCallList { get; set; } =
            new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
    }
}