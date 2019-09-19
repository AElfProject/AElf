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
using Xunit.Abstractions;

namespace AElf.Contract.TestContract
{
    public sealed class ContractBasicTests : TestContractTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IMiningService _miningService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly ITransactionResultManager _transactionResultManager;
        public ContractBasicTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _miningService = GetRequiredService<IMiningService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _smartContractExecutiveService = GetRequiredService<ISmartContractExecutiveService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
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
        public async Task DeployContract_With_Two_Branch()
        {    
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var startBlockHeight = blockHeader.Height;
            var startBlockHash = blockHeader.GetHash();

             Address contractAddress;
            //branch one
            {
                var t = (await BasicContractZeroStub.DeploySmartContract.SendAsync(
                    new Acs0.ContractDeploymentInput
                    {
                        Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicFunctionWithParallel")).Value),
                        Category = KernelConstants.CodeCoverageRunnerCategory
                    }
                ));
                var transactionResult = t.TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                contractAddress = Address.Parser.ParseFrom(transactionResult.ReturnValue);
                blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();

                var queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
                {
                    First = SampleAddress.AddressList[0],
                    Second = SampleAddress.AddressList[1]
                }.ToByteString();
                var queryTwoUserWinMoneyTransaction = CreateTransaction(DefaultSender, contractAddress,
                    "QueryTwoUserWinMoney", queryTwoUserWinMoneyInput, blockHeader.Height, blockHeader.GetHash());
                var branchOneBlock = await ExecuteAsync(queryTwoUserWinMoneyTransaction, blockHeader.Height,
                    blockHeader.GetHash());
                await _blockAttachService.AttachBlockAsync(branchOneBlock);

                 var queryTwoUserWinMoneyTransactionResult =
                    await _transactionResultManager.GetTransactionResultAsync(queryTwoUserWinMoneyTransaction
                        .GetHash(),branchOneBlock.Header.GetHash());
                queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            //branch two
            {
                var transaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                    nameof(TestBasicFunctionContractStub.QueryWinMoney), new Empty().ToByteString(), startBlockHeight,
                    startBlockHash);
                var branchTwoBlock = await ExecuteAsync(transaction, startBlockHeight, startBlockHash);
                await _blockAttachService.AttachBlockAsync(branchTwoBlock);

                 transaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                    nameof(TestBasicFunctionContractStub.QueryWinMoney), new Empty().ToByteString(), branchTwoBlock.Height,
                    branchTwoBlock.GetHash());
                branchTwoBlock = await ExecuteAsync(transaction, branchTwoBlock.Height, branchTwoBlock.GetHash());
                await _blockAttachService.AttachBlockAsync(branchTwoBlock);

                 transaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                    nameof(TestBasicFunctionContractStub.QueryWinMoney), new Empty().ToByteString(), branchTwoBlock.Height,
                    branchTwoBlock.GetHash());
                branchTwoBlock = await ExecuteAsync(transaction, branchTwoBlock.Height, branchTwoBlock.GetHash());
                await _blockAttachService.AttachBlockAsync(branchTwoBlock);

                 var queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
                {
                    First = SampleAddress.AddressList[0],
                    Second = SampleAddress.AddressList[1]
                }.ToByteString();
                var queryTwoUserWinMoneyTransaction = CreateTransaction(DefaultSender, contractAddress,
                    "QueryTwoUserWinMoney", queryTwoUserWinMoneyInput, branchTwoBlock.Height, branchTwoBlock.GetHash());

                 branchTwoBlock = await ExecuteAsync(queryTwoUserWinMoneyTransaction, branchTwoBlock.Height,
                    branchTwoBlock.GetHash());
                await _blockAttachService.AttachBlockAsync(branchTwoBlock);
                var queryTwoUserWinMoneyTransactionResult =
                    await _transactionResultManager.GetTransactionResultAsync(queryTwoUserWinMoneyTransaction.GetHash(),
                        branchTwoBlock.Header.GetPreMiningHash());
                queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                queryTwoUserWinMoneyTransactionResult.Error.ShouldContain("Invalid contract address");
            }
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

                var basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
                //execute new action method
                var transactionResult1 = (await basic11ContractStub.UpdateStopBet.SendAsync(
                    new Empty())).TransactionResult;
                transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);

                //call new view method
                var result = (await basic11ContractStub.QueryBetStatus.CallAsync(
                    new Empty())).BoolValue;
                result.ShouldBeTrue();
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
            var transactionResult1 = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call new view method
            var result = (await basic11ContractStub.QueryBetStatus.CallAsync(
                new Empty())).BoolValue;
            result.ShouldBeTrue();

            await _blockchainService.SetIrreversibleBlockAsync(chain, block.Height, block.GetHash());
        }

        [Fact]
        public async Task UpdateContract_With_Two_Branch()
        {    
            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var startBlockHeight = blockHeader.Height;
            var startBlockHash = blockHeader.GetHash();
            
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new Acs0.ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value) 
                }
            )).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
//            //execute new action method
            var transactionResult1 = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var transaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                nameof(TestBasicFunctionContractStub.QueryWinMoney), new Empty().ToByteString(), startBlockHeight, startBlockHash);
            var block = await ExecuteAsync(transaction, startBlockHeight, startBlockHash);
            await _blockAttachService.AttachBlockAsync(block);

            transaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                nameof(TestBasicFunctionContractStub.QueryWinMoney), new Empty().ToByteString(), block.Height,
                block.GetHash());
            block = await ExecuteAsync(transaction, block.Height, block.GetHash());
            await _blockAttachService.AttachBlockAsync(block);
            
            var input = new Empty().ToByteString();
            var failedTransaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
                nameof(basic11ContractStub.UpdateStopBet), input, block.Height, block.GetHash());
            block = await ExecuteAsync(failedTransaction, block.Height, block.GetHash());
            await _blockAttachService.AttachBlockAsync(block);

            transactionResult =
                await _transactionResultManager.GetTransactionResultAsync(failedTransaction.GetHash(),
                    block.Header.GetPreMiningHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Failed to find handler for UpdateStopBet.");
            
            input = new Acs0.ContractUpdateInput
            {
                Address = BasicFunctionContractAddress,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value)
            }.ToByteString();
            var updateTransaction = CreateTransaction(DefaultSender, ContractZeroAddress,
                nameof(BasicContractZeroStub.UpdateSmartContract), input, block.Height, block.GetHash());
            var updateBlock = await ExecuteAsync(updateTransaction, block.Height, block.GetHash());
            await _blockAttachService.AttachBlockAsync(updateBlock);
//            
            transactionResult =
                await _transactionResultManager.GetTransactionResultAsync(updateTransaction.GetHash(),
                    updateBlock.Header.GetPreMiningHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Code is not changed").ShouldBeTrue();
            
            input = new Acs0.ContractUpdateInput
            {
                Address = BasicFunctionContractAddress,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value)
            }.ToByteString();
            updateTransaction = CreateTransaction(DefaultSender, ContractZeroAddress,
                nameof(BasicContractZeroStub.UpdateSmartContract), input, updateBlock.Height, updateBlock.GetHash());
            updateBlock = await ExecuteAsync(updateTransaction, updateBlock.Height, updateBlock.GetHash());
            await _blockAttachService.AttachBlockAsync(updateBlock);

            transactionResult =
                await _transactionResultManager.GetTransactionResultAsync(updateTransaction.GetHash(),
                    updateBlock.Header.GetPreMiningHash());
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
           //execute new action method
            transactionResult = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call new view method
            var result = (await basic11ContractStub.QueryBetStatus.CallAsync(
                new Empty())).BoolValue;
            result.ShouldBeTrue();
            
        }

//        [Fact]
//        public async Task UpdateContract_With_Two_Different_Contract_Code()
//        {
//            var blockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
//            var startBlockHeight = blockHeader.Height;
//            var startBlockHash = blockHeader.GetHash();
//
//            var basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
//            //First branch
//            {
//                var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
//                    new Acs0.ContractUpdateInput
//                    {
//                        Address = BasicFunctionContractAddress,
//                        Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicUpdate")).Value) 
//                    }
//                )).TransactionResult;
//                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//
//                transactionResult = (await basic11ContractStub.UpdateStopBet.SendAsync(
//                    new Empty())).TransactionResult;
//                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
//
//            //Second branch
//            {
//                var input = new Acs0.ContractUpdateInput
//                {
//                    Address = BasicFunctionContractAddress,
//                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.EndsWith("BasicFunctionWithParallel")).Value)
//                }.ToByteString();
//                var transaction = CreateTransaction(DefaultSender, ContractZeroAddress,
//                    nameof(BasicContractZeroStub.UpdateSmartContract), input, startBlockHeight, startBlockHash);
//                var branchTwoBlock = await ExecuteAsync(transaction, startBlockHeight, startBlockHash);
//                await _blockAttachService.AttachBlockAsync(branchTwoBlock);
//                
//                var basicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
//                await basicFunctionContractStub.QueryWinMoney.CallAsync(new Empty());
//                
//                _smartContractExecutiveService.ClearContractInfoCache(100);
//
//                var queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
//                {
//                    First = SampleAddress.AddressList[0],
//                    Second = SampleAddress.AddressList[1]
//                }.ToByteString();
//                var queryTwoUserWinMoneyTransaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
//                    "QueryTwoUserWinMoney", queryTwoUserWinMoneyInput, branchTwoBlock.Height, branchTwoBlock.GetHash());
//                branchTwoBlock = await ExecuteAsync(queryTwoUserWinMoneyTransaction, branchTwoBlock.Height,
//                    branchTwoBlock.GetHash());
//                await _blockAttachService.AttachBlockAsync(branchTwoBlock);
//
//                var queryTwoUserWinMoneyTransactionResult =
//                    await _transactionResultManager.GetTransactionResultAsync(queryTwoUserWinMoneyTransaction.GetHash(),
//                        branchTwoBlock.Header.GetPreMiningHash());
//                queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//                queryTwoUserWinMoneyTransactionResult.Error.ShouldContain("Failed to find handler for QueryTwoUserWinMoney");
//
//                await _smartContractExecutiveService.InitContractInfoCacheAsync();
//                
//                queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
//                {
//                    First = SampleAddress.AddressList[0],
//                    Second = SampleAddress.AddressList[1]
//                }.ToByteString();
//                queryTwoUserWinMoneyTransaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
//                    "QueryTwoUserWinMoney", queryTwoUserWinMoneyInput, branchTwoBlock.Height, branchTwoBlock.GetHash());
//                branchTwoBlock = await ExecuteAsync(queryTwoUserWinMoneyTransaction, branchTwoBlock.Height,
//                    branchTwoBlock.GetHash());
//                await _blockAttachService.AttachBlockAsync(branchTwoBlock);
//                queryTwoUserWinMoneyTransactionResult =
//                    await _transactionResultManager.GetTransactionResultAsync(queryTwoUserWinMoneyTransaction
//                        .GetHash(), branchTwoBlock.Header.GetPreMiningHash());
//                queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
//
//            //Third branch
//            {
//                var updateStopBetTransaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
//                    nameof(basic11ContractStub.UpdateStopBet), new Empty().ToByteString(), startBlockHeight, startBlockHash);
//                var branchThreeBlock = await ExecuteAsync(updateStopBetTransaction, startBlockHeight, startBlockHash);
//                await _blockAttachService.AttachBlockAsync(branchThreeBlock);
//
//                var updateStopBetTransactionResult =
//                    await _transactionResultManager.GetTransactionResultAsync(updateStopBetTransaction.GetHash(),
//                        branchThreeBlock.Header.GetPreMiningHash());
//                updateStopBetTransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//                updateStopBetTransactionResult.Error.ShouldContain("Failed to find handler for UpdateStopBet.");
//            
//                var queryTwoUserWinMoneyInput = new QueryTwoUserWinMoneyInput
//                {
//                    First = SampleAddress.AddressList[0],
//                    Second = SampleAddress.AddressList[1]
//                }.ToByteString();
//                var queryTwoUserWinMoneyTransaction = CreateTransaction(DefaultSender, BasicFunctionContractAddress,
//                    "QueryTwoUserWinMoney", queryTwoUserWinMoneyInput, branchThreeBlock.Height, branchThreeBlock.GetHash());
//                branchThreeBlock = await ExecuteAsync(queryTwoUserWinMoneyTransaction, branchThreeBlock.Height,
//                    branchThreeBlock.GetHash());
//                await _blockAttachService.AttachBlockAsync(branchThreeBlock);
//
//                var queryTwoUserWinMoneyTransactionResult =
//                    await _transactionResultManager.GetTransactionResultAsync(queryTwoUserWinMoneyTransaction.GetHash(),
//                        branchThreeBlock.Header.GetPreMiningHash());
//                queryTwoUserWinMoneyTransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//                queryTwoUserWinMoneyTransactionResult.Error.ShouldContain("Failed to find handler for QueryTwoUserWinMoney.");
//            }
//
//        }

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