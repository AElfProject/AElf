using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.TestBase;
using AElf.Types.CSharp;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainContractTestBase : AElfIntegratedTest<CrossChainContractTestAElfModule>
    {
        protected ContractTester ContractTester;
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ConsensusContractAddress;
        protected Address AuthorizationContractAddress;
        public CrossChainContractTestBase()
        {
            ContractTester = new ContractTester(0, CrossChainContractTestHelper.EcKeyPair);
            AsyncHelper.RunSync(() => ContractTester.InitialChainAsync(ContractTester.GetDefaultContractTypes().ToArray()));
            CrossChainContractAddress = ContractTester.DeployedContractsAddresses[(int) ContractConsts.CrossChainContract];
            TokenContractAddress = ContractTester.DeployedContractsAddresses[(int) ContractConsts.TokenContract];
            ConsensusContractAddress = ContractTester.DeployedContractsAddresses[(int) ContractConsts.ConsensusContract];
            AuthorizationContractAddress =
                ContractTester.DeployedContractsAddresses[(int) ContractConsts.ConsensusContract];
        }

        protected async Task ApproveBalance(ulong amount)
        {
            var callOwner = Address.FromPublicKey(CrossChainContractTestHelper.GetPubicKey());

            var approveResult = await ContractTester.ExecuteContractWithMiningAsync(TokenContractAddress, "Approve",
                CrossChainContractAddress, amount);
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await ContractTester.CallContractMethodAsync(TokenContractAddress, "Allowance",
                callOwner, CrossChainContractAddress);
        }

        protected async Task Initialize(ulong tokenAmount)
        {
            var tx1 = ContractTester.GenerateTransaction(TokenContractAddress, "Initialize",
                "ELF", "elf token", tokenAmount, 2U);
            var tx2 = ContractTester.GenerateTransaction(CrossChainContractAddress, "Initialize",
                ConsensusContractAddress, TokenContractAddress, AuthorizationContractAddress);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1, tx2});
        }
    }
}