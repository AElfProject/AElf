using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public sealed class ExecutionPluginForMethodFeeWithForkTest : ExecutionPluginForMethodFeeWithForkTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ExecutionPluginForMethodFeeWithForkTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task ChargeFee_With_Fork_Test()
        {
            var amount = 100000;

            await SetMethodFeeWithProposalAsync(new MethodFees
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = "ELF",
                        BasicFee = 100000
                    }
                }
            }.ToByteString());
            
            var result = await Tester.ExecuteContractWithMiningReturnBlockAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Transfer), new TransferInput
                {
                    Amount = amount,
                    Symbol = "ELF",
                    Memo = Guid.NewGuid().ToString(),
                    To = SampleAddress.AddressList[0]
                });
            var transactionResult = await Tester.GetTransactionResultAsync(result.Item2.GetHash());
            var targetFee = transactionResult.GetChargedTransactionFees().First().Value;
            
            var transactionFeesMap = await Tester.GetTransactionFeesMapAsync(new ChainContext
            {
                BlockHash = result.Item1.GetHash(), BlockHeight = result.Item1.Height
            });
            transactionFeesMap.First().Value.ShouldBe(targetFee);
            var chain = await Tester.GetChainAsync();

            // branch one
            {
                var branchOneBlock = await Tester.MineEmptyBlockAsync();
            
                transactionFeesMap = await Tester.GetTransactionFeesMapAsync(new ChainContext
                {
                    BlockHash = branchOneBlock.GetHash(), BlockHeight = branchOneBlock.Height
                });
                transactionFeesMap.ShouldBeNull();
                await SetMethodFeeWithProposalAsync(new MethodFees
                {
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "ELF",
                            BasicFee = 200000
                        }
                    }
                }.ToByteString());
            
                result = await Tester.ExecuteContractWithMiningReturnBlockAsync(TokenContractAddress,
                    nameof(TokenContractContainer.TokenContractStub.Transfer), new TransferInput
                    {
                        Amount = amount,
                        Symbol = "ELF",
                        Memo = Guid.NewGuid().ToString(),
                        To = SampleAddress.AddressList[0]
                    });
                transactionResult = await Tester.GetTransactionResultAsync(result.Item2.GetHash());
                var fee = transactionResult.GetChargedTransactionFees().First().Value;
                transactionFeesMap = await Tester.GetTransactionFeesMapAsync(new ChainContext
                {
                    BlockHash = result.Item1.GetHash(), BlockHeight = result.Item1.Height
                });
                transactionFeesMap.First().Value.ShouldBe(fee);  //300000
                targetFee.ShouldNotBe(fee);
            }

            // branch two
            {
                var branchTwoBlocks = await GenerateEmptyBlocksAsync(13, chain.BestChainHash, chain.BestChainHeight);
                chain= await Tester.GetChainAsync();
                chain.BestChainHash.ShouldBe(result.Item1.GetHash());
                var branchTwoBlock =
                    await Tester.MineEmptyBlockAsync(branchTwoBlocks.Last().GetHash(), branchTwoBlocks.Last().Height);
                chain = await Tester.GetChainAsync();
                chain.BestChainHash.ShouldBe(branchTwoBlock.GetHash());
                
                var transactions =
                    await Tester.GetTransactionsAsync(branchTwoBlocks.First().Body.TransactionIds);
                var transaction = transactions.Single(t =>
                    t.MethodName == nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees));
                var feesMap = TotalTransactionFeesMap.Parser.ParseFrom(transaction.Params);
                feesMap.Value.First().Value.ShouldBe(targetFee);
            }
        }

        private async Task<List<Block>> GenerateEmptyBlocksAsync(int count , Hash previousBlockHash, long previousBlockHeight)
        {
            var blocks = new List<Block>();
            var block = await Tester.MineEmptyBlockAsync(previousBlockHash, previousBlockHeight);
            blocks.Add(block.Block);
            for (var i = 0; i < count - 1; i++)
            {
                block = await Tester.MineEmptyBlockAsync(block.GetHash(), block.Height);
                blocks.Add(block.Block);
            }

            return blocks;
        }
    }
}