using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests
{
    public partial class ExecutionPluginForResourceFeeTest
    {
        [Fact]
        public async Task GenerateTransactions_NoneTransaction_Generate_Test()
        {
            // block height == 0
            var systemTransactionGenerator =
                GetCreateInstance<ISystemTransactionGenerator, DonateResourceTransactionGenerator>();
            systemTransactionGenerator.ShouldNotBeNull();
            var transactions = await systemTransactionGenerator.GenerateTransactionsAsync(DefaultSender, 0, new Hash());
            transactions.Count.ShouldBe(0);

            await AdvanceResourceToken();
            await TestContractStub.CpuConsumingMethod.SendAsync(new Empty());
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = "ELF",
                Amount = 1000,
                Spender = TokenConverterAddress
            });

            //no new token fee information
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var blockHeader = await bcs.GetBestChainLastBlockHeaderAsync();
            transactions =
                await systemTransactionGenerator.GenerateTransactionsAsync(DefaultSender, blockHeader.Height,
                    blockHeader.GetHash());
            transactions.Count.ShouldBe(1);
            var systemTransaction = transactions[0];
            systemTransaction.MethodName.ShouldBe(nameof(TokenContractStub.DonateResourceToken));
            var tokenFeeInfo = TotalResourceTokensMaps.Parser.ParseFrom(systemTransaction.Params);
            tokenFeeInfo.Value.Count.ShouldBe(0);
        }


        [Fact]
        public async Task GenerateTransactions_Success_Test()
        {
            const string symbol = "READ";
            await AdvanceResourceToken();
            await TestContractStub.CpuConsumingMethod.SendAsync(new Empty());
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var blockHeader = await bcs.GetBestChainLastBlockHeaderAsync();
            var block = await bcs.GetBlockByHashAsync(blockHeader.GetHash());
            var systemTransactionGenerator =
                GetCreateInstance<ISystemTransactionGenerator, DonateResourceTransactionGenerator>();
            systemTransactionGenerator.ShouldNotBeNull();
            var transactions =
                await systemTransactionGenerator.GenerateTransactionsAsync(DefaultSender, blockHeader.Height,
                    blockHeader.GetHash());
            transactions.Count.ShouldBe(1);
            var donateResourceTransaction = transactions[0];
            donateResourceTransaction.MethodName.ShouldBe(nameof(TokenContractContainer.TokenContractStub
                .DonateResourceToken));
            var txFeeMap = TotalTransactionFeesMap.Parser.ParseFrom(donateResourceTransaction.Params);
            txFeeMap.Value.ContainsKey(symbol);

            var transactionValidations = Application.ServiceProvider
                .GetRequiredService<IEnumerable<IBlockValidationProvider>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per type
            var claimTransactionValidation =
                transactionValidations.SingleOrDefault(
                    p => p.GetType() == typeof(DonateResourceTokenValidationProvider));
            claimTransactionValidation.ShouldNotBeNull();
            var validateRet = await claimTransactionValidation.ValidateBlockAfterExecuteAsync(block);
            validateRet.ShouldBe(true);
        }


        [Fact]
        public async Task ValidateBlockAfterExecute_Without_ResourceTokenFee_Test()
        {
            // without resource token fee
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var blockHeader = await bcs.GetBestChainLastBlockHeaderAsync();
            var block = await bcs.GetBlockByHashAsync(blockHeader.GetHash());
            var claimTransactionValidation = GetCreateInstance<IBlockValidationProvider, DonateResourceTokenValidationProvider>();
            claimTransactionValidation.ShouldNotBeNull();
            var validateRet = await claimTransactionValidation.ValidateBlockAfterExecuteAsync(block);
            validateRet.ShouldBe(true);

            // with invalid block height
            block.Header.Height = block.Header.Height.Add(1);
            validateRet = await claimTransactionValidation.ValidateBlockAfterExecuteAsync(block);
            validateRet.ShouldBe(false);
        }

        [Fact]
        public async Task GetPreTransactions_None_PreTransaction_Generate_Test()
        {
            var plugin = GetCreateInstance<IPreExecutionPlugin, ResourceConsumptionPreExecutionPlugin>();
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();

            // invalid contract descriptor
            var transactions = (await plugin.GetPreTransactionsAsync(TokenConverterContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TokenConverterAddress,
                        MethodName = nameof(TokenConverterContractStub.SetConnector)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);

            // token contract address and method name is ChargeResourceToken
            transactions = (await plugin.GetPreTransactionsAsync(TokenContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TokenContractAddress,
                        MethodName = nameof(TokenContractStub.ChargeResourceToken)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);

            //method name is BuyResourceToken
            transactions = (await plugin.GetPreTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TestContractAddress,
                        MethodName = "BuyResourceToken"
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);
        }


        [Fact]
        public async Task GetPreTransactions_Success_Test()
        {
            var plugin = GetCreateInstance<IPreExecutionPlugin, ResourceConsumptionPreExecutionPlugin>();
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();

            var transactions = (await plugin.GetPreTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = TestContractAddress,
                        To = TokenConverterAddress,
                        MethodName = nameof(TestContractStub.CpuConsumingMethod)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(1);
            var preTransaction = transactions[0];
            preTransaction.MethodName.ShouldBe(nameof(TokenContractStub.CheckResourceToken));
        }

        [Fact]
        public async Task GetPreTransactions_None_PostTransaction_Generate_Test()
        {
            var plugin = GetCreateInstance<IPostExecutionPlugin, ResourceConsumptionPostExecutionPlugin>();
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();

            // invalid contract descriptor
            var transactions = (await plugin.GetPostTransactionsAsync(TokenConverterContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TokenConverterAddress,
                        MethodName = nameof(TokenConverterContractStub.SetConnector)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);

            // token contract address and method name is ChargeResourceToken
            transactions = (await plugin.GetPostTransactionsAsync(TokenContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TokenContractAddress,
                        MethodName = nameof(TokenContractStub.ChargeResourceToken)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);

            //method name is BuyResourceToken
            transactions = (await plugin.GetPostTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TestContractAddress,
                        MethodName = "BuyResourceToken"
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GetPostTransactions_Success_Test()
        {
            const string symbol = "READ";
            var plugin = GetCreateInstance<IPostExecutionPlugin, ResourceConsumptionPostExecutionPlugin>();
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();
            var transactionContext = new TransactionContext
            {
                Transaction = new Transaction
                {
                    From = TestContractAddress,
                    To = TokenConverterAddress,
                    MethodName = nameof(TestContractStub.CpuConsumingMethod)
                },
                BlockHeight = chain.BestChainHeight + 1,
                PreviousBlockHash = chain.BestChainHash
            };
            transactionContext.Trace = new TransactionTrace
            {
                StateSet = new TransactionExecutingStateSet()
            };
            var stateCounts = transactionContext.Trace.StateSet;
            stateCounts.Reads["read"] = true;
            var transactions = (await plugin.GetPostTransactionsAsync(TestContract.ContractContainer.Descriptors,
                transactionContext)).ToList();

            transactions.Count.ShouldBe(1);
            var postTransaction = transactions[0];
            postTransaction.MethodName.ShouldBe(nameof(TokenContractStub.ChargeResourceToken));
            var tokenFeeInfo = ChargeResourceTokenInput.Parser.ParseFrom(postTransaction.Params);
            tokenFeeInfo.CostDic.ContainsKey(symbol);
            tokenFeeInfo.CostDic[symbol].ShouldBeGreaterThan(0);
        }

        private I GetCreateInstance<I, T>() where T : I
        {
            var implements = Application.ServiceProvider.GetRequiredService<IEnumerable<I>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per t
            var implement = implements.SingleOrDefault(p => p.GetType() == typeof(T));
            return implement;
        }
    }
}