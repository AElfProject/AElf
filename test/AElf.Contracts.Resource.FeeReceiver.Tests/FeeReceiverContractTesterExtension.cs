using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;

namespace AElf.Contracts.Resource.FeeReceiver
{
    internal class Dummy{}
    public static class FeeReceiverContractTesterExtension
    {
        private static IReadOnlyDictionary<string, byte[]> _codes;

        public static IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<Dummy>());

        public static byte[] TokenContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("MultiToken")).Value;
        public static byte[] FeeReceiverContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("FeeReceiver")).Value;

        public static async Task InitialChainAndTokenAsync(this ContractTester<FeeReceiverContractTestAElfModule> starter)
        {
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000L,
                Issuer = starter.GetCallOwnerAddress()
            });
            
            // For testing.
            tokenContractCallList.Add(nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
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
//                    list.AddGenesisSmartContract(
//                        DividendContractCode,
//                        DividendSmartContractAddressNameProvider.Name);
                    list.AddGenesisSmartContract(
                        TokenContractCode,
                        TokenSmartContractAddressNameProvider.Name,
                        tokenContractCallList);
                    list.AddGenesisSmartContract(
                        FeeReceiverContractCode,
                        ResourceFeeReceiverSmartContractAddressNameProvider.Name);
                });
        }
    }
}