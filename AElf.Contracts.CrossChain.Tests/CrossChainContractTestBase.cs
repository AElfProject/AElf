using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Authorization;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.TestBase;
using Google.Protobuf;
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
            CrossChainContractAddress = ContractTester.GetContractAddress(Hash.FromString(typeof(CrossChainContract).FullName));
            TokenContractAddress = ContractTester.GetContractAddress(Hash.FromString(typeof(TokenContract).FullName));
            ConsensusContractAddress = ContractTester.GetContractAddress(Hash.FromString(typeof(ConsensusContract).FullName));
            AuthorizationContractAddress = ContractTester.GetContractAddress(Hash.FromString(typeof(AuthorizationContract).FullName));
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

        protected async Task Initialize(ulong tokenAmount, int parentChainId = 0)
        {
            var tx1 = ContractTester.GenerateTransaction(TokenContractAddress, "Initialize",
                "ELF", "elf token", tokenAmount, 2U);
            var tx2 = ContractTester.GenerateTransaction(CrossChainContractAddress, "Initialize",
                ConsensusContractAddress, TokenContractAddress, AuthorizationContractAddress,
                parentChainId == 0 ? ChainHelpers.GetRandomChainId() : parentChainId);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1, tx2});
        }

        protected async Task<int> InitAndCreateSideChain(int parentChainId = 0, ulong lockedTokenAmount = 10)
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
            
            var tx1 = ContractTester.GenerateTransaction(CrossChainContractAddress, "RequestChainCreation",
                sideChainInfo);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx1});
            var chainId = ChainHelpers.GetChainId(1);
            var tx2 = ContractTester.GenerateTransaction(CrossChainContractAddress, "CreateSideChain", chainId);
            await ContractTester.MineABlockAsync(new List<Transaction> {tx2});
            return chainId;
        }

        protected async Task<Block> MineAsync(List<Transaction> txs, List<Transaction> systemTxs = null)
        {
            return await ContractTester.MineABlockAsync(txs, systemTxs);
        }
        
        protected  async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName, params object[] objects)
        {
            return await ContractTester.ExecuteContractWithMiningAsync(contractAddress, methodName, objects);
        }

        protected Transaction GenerateTransaction(Address contractAddress, string methodName, ECKeyPair ecKeyPair = null, params object[] objects)
        {
            return ecKeyPair == null
                ? ContractTester.GenerateTransaction(contractAddress, methodName, objects)
                : ContractTester.GenerateTransaction(contractAddress, methodName, ecKeyPair, objects);
        }

        protected async Task<TransactionResult> GetTransactionResult(Hash txId)
        {
            return await ContractTester.GetTransactionResult(txId);
        }

        protected async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            return await ContractTester.CallContractMethodAsync(contractAddress, methodName, objects);
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