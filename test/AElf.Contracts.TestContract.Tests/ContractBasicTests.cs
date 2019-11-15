using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Contracts.TestKit;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public sealed class ContractBasicTests : TestContractTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IMiningService _miningService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        
        public ContractBasicTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _miningService = GetRequiredService<IMiningService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _smartContractExecutiveProvider = GetRequiredService<ISmartContractExecutiveProvider>();
            InitializeTestContracts();
        }

        [Fact]
        public async Task Initialize_MultiTimesContract_Test()
        {
            var transactionResult = (await TestBasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput
                {
                    ContractName = "Test initialize again",
                    MinValue = 1000,
                    MaxValue = 10000,
                    Manager = SampleAddress.AddressList[0]
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task DeployContract()
        {
            var transactionResult = (await BasicContractZeroStub.DeploySmartContract.SendAsync(
                new Acs0.ContractDeploymentInput
                {
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicFunctionWithParallel")).Value),
                    Category = KernelConstants.CodeCoverageRunnerCategory
                }
            )).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var contractAddress = Address.Parser.ParseFrom(transactionResult.ReturnValue);
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();

            var basicFunctionWithParallelContractStub =
                GetTester<BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub>(
                    contractAddress, DefaultSenderKeyPair);
            var queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
            {
                First = SampleAddress.AddressList[0],
                Second = SampleAddress.AddressList[1]
            };
            var queryTwoUserWinMoneyTransactionResult = (await basicFunctionWithParallelContractStub.QueryTwoUserWinMoney.SendAsync(queryTwoUserWinMoneyInput)).TransactionResult;
            queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            queryTwoUserWinMoneyTransactionResult.Error.ShouldContain("Invalid contract address.");
            _smartContractExecutiveProvider.SetIrreversedCache(blockHeader.GetHash());
            queryTwoUserWinMoneyTransactionResult = (await basicFunctionWithParallelContractStub.QueryTwoUserWinMoney.SendAsync(queryTwoUserWinMoneyInput)).TransactionResult;
            queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task UpdateContract_WithOwner_Test()
        {
            //update with same code
            {
                var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                    new Acs0.ContractUpdateInput
                    {
                        Address = BasicFunctionContractAddress,
                        Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value)
                    }
                )).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Code is not changed").ShouldBeTrue();
            }
            
            //different code
            {
                var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                    new Acs0.ContractUpdateInput
                    {
                        Address = BasicFunctionContractAddress,
                        Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value)
                    }
                )).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task UpdateContract_And_Call_Old_Method_Test()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new Acs0.ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value)
                }
            )).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //execute new action method
            transactionResult = (await TestBasicFunctionContractStub.UserPlayBet.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.BetInput
                {
                    Int64Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var winData = (await TestBasicFunctionContractStub.QueryUserWinMoney.CallAsync(
                DefaultSender)).Int64Value;
            if (winData > 0)
            {
                winData.ShouldBeGreaterThanOrEqualTo(100);
                return;
            }

            var loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            (winData + loseData).ShouldBe(100);
            
            //execute again
            transactionResult = (await TestBasicFunctionContractStub.UserPlayBet.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.BetInput
                {
                    Int64Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            //check result
            loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            (winData + loseData).ShouldBe(200);
        }

        [Fact]
        public async Task ChangeContractAuthor_Test()
        {
            //without permission
            {
                var testUser = SampleECKeyPairs.KeyPairs[2];
                var otherZeroStub = GetContractZeroTester(testUser);
                var transactionResult = (await otherZeroStub.ChangeContractAuthor.SendAsync(
                    new Acs0.ChangeContractAuthorInput()
                    {
                        ContractAddress = BasicFunctionContractAddress,
                        NewAuthor = SampleAddress.AddressList[1]
                    }
                )).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("no permission").ShouldBeTrue();
            }
            
            //with permission
            {
                var otherUser = SampleAddress.AddressList[2];
                var transactionResult = (await BasicContractZeroStub.ChangeContractAuthor.SendAsync(
                    new Acs0.ChangeContractAuthorInput()
                    {
                        ContractAddress = BasicFunctionContractAddress,
                        NewAuthor = otherUser
                    }
                )).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var ownerAddress =
                    (await BasicContractZeroStub.GetContractAuthor.CallAsync(BasicFunctionContractAddress))
                    .GetFormatted();
                ownerAddress.ShouldBe(otherUser.GetFormatted());
            }
        }
        
        [Fact]
        public async Task UpdateContract_Attach_After_ReadOnly_Transaction()
        {
            var chain = await _blockchainService.GetChainAsync();
            var blockHeight = chain.BestChainHeight;
            var blockHash = chain.BestChainHash;
            
            var input = new Acs0.ContractUpdateInput
            {
                Address = BasicFunctionContractAddress,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value)
            }.ToByteString();
            var transaction = CreateTransaction(DefaultSender, ContractZeroAddress,
                nameof(BasicContractZeroStub.UpdateSmartContract), input, blockHeight, blockHash);
            var block = await ExecuteAsync(transaction, blockHeight, blockHash);

            var basicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
            await basicFunctionContractStub.QueryWinMoney.CallAsync(new Empty());
            
            await _blockAttachService.AttachBlockAsync(block);
            
            var basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
//            //execute new action method
            var updateStopBetResult = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            updateStopBetResult.Status.ShouldBe(TransactionResultStatus.Failed);
            updateStopBetResult.Error.ShouldContain("Failed to find handler for UpdateStopBet.");

            _smartContractExecutiveProvider.SetIrreversedCache(block.GetHash());
            
            updateStopBetResult = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            updateStopBetResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call new view method
            var result = (await basic11ContractStub.QueryBetStatus.CallAsync(
                new Empty())).BoolValue;
            result.ShouldBeTrue();

            await _blockchainService.SetIrreversibleBlockAsync(chain, block.Height, block.GetHash());
        }

        private Transaction CreateTransaction(Address from, Address to, string methodName,
            ByteString parameters, long blockHeight, Hash blockHash)
        {
            var transaction = new Transaction()
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = parameters,
                RefBlockNumber = blockHeight,
                RefBlockPrefix = ByteString.CopyFrom(blockHash.Value.Take(4).ToArray()),
            };
            var signature = CryptoHelper.SignWithPrivateKey(DefaultSenderKeyPair.PrivateKey,
                transaction.GetHash().Value.ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            return transaction;
        }

        private async Task<Block> ExecuteAsync(Transaction transaction,long previousBlockHeight,Hash previousBlockHash)
        {
            var transactionList = new List<Transaction>();
            if(transaction!=null) transactionList.Add(transaction);
            var block = await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = previousBlockHash, PreviousBlockHeight = previousBlockHeight,
                    BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue)
                },
                transactionList,
                DateTime.UtcNow.ToTimestamp());

            if(transaction != null)
                await _blockchainService.AddTransactionsAsync(new List<Transaction> {transaction});
            await _blockchainService.AddBlockAsync(block);
            return block;
        }
    }
}