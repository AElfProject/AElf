using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Indexing.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class CrossChainIndexingTransactionGeneratorTest : CrossChainTestBase
    {
        private readonly ISystemTransactionGenerator _crossChainIndexingTransactionGenerator;
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainIndexingTransactionGeneratorTest()
        {
            _crossChainIndexingTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _crossChainIndexingDataService = GetRequiredService<ICrossChainIndexingDataService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        [Fact]
        public async Task GenerateTransactions_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<IBlockCacheEntity>();
            var previousBlockHash = Hash.FromString("PreviousBlockHash");
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
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                };
                sideChainBlockInfoCache.Add(sideChainBlockData);
                if (i <= CrossChainConstants.DefaultBlockCacheEntityCount)
                    crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideChainBlockInfoCache}});
            
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);

            transactions.Count.ShouldBe(1);
            transactions[0].From.ShouldBe(SampleAddress.AddressList[0]);
            transactions[0].To.ShouldBe(smartContractAddress);
            transactions[0].RefBlockNumber.ShouldBe(previousBlockHeight);
            
            transactions[0].RefBlockPrefix.ShouldBe(BlockHelper.GetRefBlockPrefix(previousBlockHash));
            transactions[0].MethodName
                .ShouldBe(nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing));

            var crossChainBlockDataInParam = CrossChainBlockData.Parser.ParseFrom(transactions[0].Params);
            Assert.Equal(crossChainBlockData, crossChainBlockDataInParam);
        }
        
        [Fact]
        public async Task GenerateTransaction_PendingProposal_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<IBlockCacheEntity>();
            var previousBlockHash = Hash.FromString("PreviousBlockHash");
            var previousBlockHeight = 1;
            
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                };
                sideChainBlockInfoCache.Add(sideChainBlockData);
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideChainBlockInfoCache}});

            var pendingProposal = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = Hash.FromString("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData(),
                ToBeReleased = true,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingProposal);
            
            
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);

            transactions.Count.ShouldBe(1);
            transactions[0].From.ShouldBe(SampleAddress.AddressList[0]);
            transactions[0].To.ShouldBe(smartContractAddress);
            transactions[0].RefBlockNumber.ShouldBe(previousBlockHeight);
            transactions[0].RefBlockPrefix.ShouldBe(BlockHelper.GetRefBlockPrefix(previousBlockHash));
            transactions[0].MethodName
                .ShouldBe(nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing));

            var proposalIdInParam = Hash.Parser.ParseFrom(transactions[0].Params);
            Assert.Equal(pendingProposal.ProposalId, proposalIdInParam);
        }
        
        [Fact]
        public async Task GenerateTransaction_PendingProposal_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<IBlockCacheEntity>();
            var previousBlockHash = Hash.FromString("PreviousBlockHash");
            var previousBlockHeight = 1;
            
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                };
                sideChainBlockInfoCache.Add(sideChainBlockData);
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideChainBlockInfoCache}});

            var pendingProposal = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = Hash.FromString("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData(),
                ToBeReleased = false,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingProposal);
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);
            Assert.Empty(transactions);
        }
        
        [Fact]
        public async Task GenerateTransaction_NoTransaction_Test()
        {
            var sideChainId = 123;
            var previousBlockHash = Hash.FromString("PreviousBlockHash");
            var previousBlockHeight = 1;
            
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);

            Assert.Empty(transactions);
        }
    }
}