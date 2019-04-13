using System.Threading.Tasks;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;

namespace AElf.Contracts.Resource.FeeReceiver
{
    public static class FeeReceiverContractTesterExtension
    {
        public static async Task InitialChainAndTokenAsync(this ContractTester<FeeReceiverContractTestAElfModule> starter)
        {
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000L,
                Issuer = starter.GetCallOwnerAddress(),
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });
            
            // For testing.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = 1000_000L,
                To = starter.GetCallOwnerAddress(),
                Memo = "Set dividends.",
            });
            
            await starter.InitialChainAsync(
                list =>
                {
                    // Dividends contract must be deployed before token contract.
                    list.AddGenesisSmartContract<DividendContract>(DividendsSmartContractAddressNameProvider.Name);
                    list.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name, tokenContractCallList);
                    list.AddGenesisSmartContract<FeeReceiverContract>(ResourceFeeReceiverSmartContractAddressNameProvider.Name);
                });
        }
    }
}