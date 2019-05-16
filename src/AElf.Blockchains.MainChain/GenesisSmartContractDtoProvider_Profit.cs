using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Profit;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForProfit(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Profit")).Value,
                ProfitSmartContractAddressNameProvider.Name, GenerateProfitInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            var profitContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            profitContractMethodCallList.Add(nameof(ProfitContractContainer.ProfitContractStub.InitializeProfitContract),
                new InitializeProfitContractInput
                {
                    // To handle tokens when release profit, add profits and receive profits.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });
            return profitContractMethodCallList;
        }
    }
}