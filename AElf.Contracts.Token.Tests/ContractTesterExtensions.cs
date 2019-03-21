using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.Token
{
    public static class ContractTesterExtensions
    {
        public static Address GetTokenContractAddress(
            this ContractTester<TokenContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }

        public static Address GetDividendsContractAddress(
            this ContractTester<TokenContractTestAElfModule> contractTester)
        {
            return contractTester.GetContractAddress(DividendsSmartContractAddressNameProvider.Name);
        }

        public static async Task TransferTokenAsync(this ContractTester<TokenContractTestAElfModule> starter, Address to,
            long amount)
        {
            await starter.ExecuteTokenContractMethodWithMiningAsync(nameof(TokenContract.Transfer), new TransferInput
            {
                Symbol = "ELF",
                Amount = amount,
                To = to
            });
        }

        public static async Task<TransactionResult> ExecuteTokenContractMethodWithMiningAsync(
            this ContractTester<TokenContractTestAElfModule> contractTester, string methodName, IMessage input)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                methodName, input);
        }

        public static async Task<long> GetBalanceAsync(this ContractTester<TokenContractTestAElfModule> contractTester,
            Address targetAddress)
        {
            var balanceOutput =GetBalanceOutput.Parser.ParseFrom(
                await contractTester.CallContractMethodAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = targetAddress,
                    Symbol = "ELF"
                }));
            return balanceOutput.Balance;
        }

        public static async Task<TransactionResult> Lock(this ContractTester<TokenContractTestAElfModule> contractTester, long amount,
            Hash lockId, Address lockToAddress = null)
        {
            if (lockToAddress == null)
            {
                lockToAddress = contractTester.GetConsensusContractAddress();
            }
            
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.Lock),
                new LockInput
                {
                    From = contractTester.GetCallOwnerAddress(),
                    To = lockToAddress,
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });
        }

        public static async Task<TransactionResult> Unlock(this ContractTester<TokenContractTestAElfModule> contractTester, long amount,
            Hash lockId)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetTokenContractAddress(),
                nameof(TokenContract.Unlock),
                new UnlockInput
                {
                    From = contractTester.GetCallOwnerAddress(),
                    To = contractTester.GetConsensusContractAddress(),
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });
        }
    }
}