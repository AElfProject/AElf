using System.Collections.Generic;
using AElf.Contracts.Profit;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForProfit(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract<ProfitContract>(
                ProfitSmartContractAddressNameProvider.Name, GenerateProfitInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            var profitContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            profitContractMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),new Empty());
            return profitContractMethodCallList;
        }
    }
}