using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividends;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;

namespace AElf.Contracts.Token
{
    public static class ContractTesterExtensions
    {
        public static Address GetTokenContractAddress(
            this ContractTester<TokenContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(typeof(TokenContract));
        }

        public static Address GetDividendsContractAddress(
            this ContractTester<TokenContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(typeof(DividendsContract));
        }

        public static async Task CreateTokenAsync(this ContractTester<TokenContractTestAElfModule> starter,
            params Address[] whiteAddresses)
        {
            // Initial token.
            await starter.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Create), new CreateInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                Issuer = starter.GetCallOwnerAddress(),
                TokenName = "elf token",
                TotalSupply = 100_000,
                LockWhiteList = {whiteAddresses}
            });
        }

        public static async Task IssueTokenAsync(this ContractTester<TokenContractTestAElfModule> starter, Address to,
            long amount)
        {
            await starter.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = amount,
                To = to
            });
        }

        public static async Task<TransactionResult> ExecuteTokenContractMethodWithMiningAsync(
            this ContractTester<TokenContractTestAElfModule> contractTester, string methodName, params object[] objects)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                methodName, objects);
        }

        public static async Task<long> GetBalanceAsync(this ContractTester<TokenContractTestAElfModule> contractTester,
            Address targetAddress)
        {
            var bytes = await contractTester.CallContractMethodAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = targetAddress,
                    Symbol = "ELF"
                });
            var balanceOutput = bytes.DeserializeToPbMessage<GetBalanceOutput>();
            return balanceOutput.Balance;
        }
    }
}