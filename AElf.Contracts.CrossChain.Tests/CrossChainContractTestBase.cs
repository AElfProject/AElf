using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Authorization;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.TestBase;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainContractTestBase : ContractTestBase<CrossChainContractTestAElfModule>
    {
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ConsensusContractAddress;
        public CrossChainContractTestBase()
        {
            Tester = new ContractTester<CrossChainContractTestAElfModule>(CrossChainContractTestHelper.EcKeyPair);
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(Tester.GetDefaultContractTypes()));
            CrossChainContractAddress = Tester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ConsensusContractAddress = Tester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
        }

        protected async Task ApproveBalance(long amount)
        {
            var callOwner = Address.FromPublicKey(CrossChainContractTestHelper.GetPubicKey());

            var approveResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Approve), new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetAllowance),
                new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = callOwner,
                    Spender = CrossChainContractAddress
                });
        }

        protected async Task Initialize(long tokenAmount, int parentChainId = 0)
        {
            var tx1 = await Tester.GenerateTransactionAsync(TokenContractAddress, nameof(TokenContract.Create),
                new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = false,
                    Issuer = Tester.GetCallOwnerAddress(),
                    TokenName = "elf token",
                    TotalSupply = tokenAmount
                });
            var tx2 = await Tester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.Initialize),
                ConsensusContractAddress, TokenContractAddress, parentChainId == 0 ? ChainHelpers.GetRandomChainId() : parentChainId);
            var tx3 = await Tester.GenerateTransactionAsync(TokenContractAddress, nameof(TokenContract.Issue),
                new IssueInput
                {
                    Symbol = "ELF",
                    Amount = tokenAmount,
                    To = Tester.GetCallOwnerAddress(),
                    Memo = "Initial tokens for testing cross chain contract."
                });
            await Tester.MineAsync(new List<Transaction> {tx1, tx2,tx3});
        }

        protected async Task<int> InitAndCreateSideChain(int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await Initialize(1000, parentChainId);

            await ApproveBalance(lockedTokenAmount);
            var sideChainInfo = new SideChainInfo
            {
                SideChainStatus = SideChainStatus.Apply,
                ContractCode = ByteString.Empty,
                IndexingPrice = 1,
                Proposer = CrossChainContractTestHelper.GetAddress(),
                LockedTokenAmount = lockedTokenAmount
            };

            var tx1 = await Tester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.RequestChainCreation), sideChainInfo);
            await Tester.MineAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = await Tester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContract.CreateSideChain), chainId);
            await Tester.MineAsync(new List<Transaction> {tx2});
            return chainId;
        }

        protected async Task<Block> MineAsync(List<Transaction> txs)
        {
            return await Tester.MineAsync(txs);
        }
        
        protected  async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName, params object[] objects)
        {
            return await Tester.ExecuteContractWithMiningAsync(contractAddress, methodName, objects);
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName, ECKeyPair ecKeyPair = null, params object[] objects)
        {
            return ecKeyPair == null
                ? await Tester.GenerateTransactionAsync(contractAddress, methodName, objects)
                : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, objects);
        }

        protected async Task<TransactionResult> GetTransactionResult(Hash txId)
        {
            return await Tester.GetTransactionResultAsync(txId);
        }

        protected async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            return await Tester.CallContractMethodAsync(contractAddress, methodName, objects);
        }
        
        protected byte[] GetFriendlyBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes.Skip(Array.FindIndex(bytes, Convert.ToBoolean)).ToArray();
        }
    }
}