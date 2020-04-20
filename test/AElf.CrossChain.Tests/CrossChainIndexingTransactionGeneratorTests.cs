using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class CrossChainIndexingTransactionGeneratorTest : CrossChainTestBase
    {
        private readonly ISystemTransactionGenerator _crossChainIndexingTransactionGenerator;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public CrossChainIndexingTransactionGeneratorTest()
        {
            _crossChainIndexingTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task GenerateTransactions_Test()
        {
            var sideChainId = 123;
            var previousBlockHash = HashHelper.ComputeFromString("PreviousBlockHash");
            var previousBlockHeight = 1;
            var crossChainBlockData = new CrossChainBlockData
            {
                PreviousBlockHeight = previousBlockHeight
            };
            
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                };
                if (i <= CrossChainConstants.DefaultBlockCacheEntityCount)
                    crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);
            }

            var crossChainTransactionInput = new CrossChainTransactionInput
            {
                Value = crossChainBlockData.ToByteString(),
                MethodName = nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
                PreviousBlockHeight = previousBlockHeight
            };
            _crossChainTestHelper.AddFakeCrossChainTransactionInput(previousBlockHash, crossChainTransactionInput);
            // AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>> {{sideChainId, sideChainBlockInfoCache}});

            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            });

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);

            transactions.Count.ShouldBe(1);
            transactions[0].From.ShouldBe(SampleAddress.AddressList[0]);
            transactions[0].To.ShouldBeNull();
            transactions[0].RefBlockNumber.ShouldBe(previousBlockHeight);
            transactions[0].RefBlockPrefix.ShouldBe(ByteString.CopyFrom(previousBlockHash.Value.Take(4).ToArray()));
            transactions[0].MethodName
                .ShouldBe(nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing));

            var crossChainBlockDataInParam = CrossChainBlockData.Parser.ParseFrom(transactions[0].Params);
            Assert.Equal(crossChainBlockData, crossChainBlockDataInParam);
        }
        
        [Fact]
        public async Task GenerateTransaction_NoTransaction_Test()
        {
            var previousBlockHash = HashHelper.ComputeFromString("PreviousBlockHash");
            var previousBlockHeight = 1;

            var smartContractAddress = SampleAddress.AddressList[0];
            await _smartContractAddressService.SetSmartContractAddressAsync(new BlockIndex
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            }, CrossChainSmartContractAddressNameProvider.StringName, smartContractAddress);
        
            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);
        
            Assert.Empty(transactions);
        }
    }
}