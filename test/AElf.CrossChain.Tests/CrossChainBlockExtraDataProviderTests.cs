using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProviderTest : CrossChainTestBase
    {
        private readonly IBlockExtraDataProvider _crossChainBlockExtraDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
    
        public CrossChainBlockExtraDataProviderTest()
        {
            _crossChainBlockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
        }
    
        [Fact]
        public async Task FillExtraData_GenesisHeight_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 1
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetBlockHeaderExtraDataAsync(header);
            Assert.Empty(bytes);
        }
    
        [Fact]
        public async Task FillExtraData__NoPendingProposal_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 2
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetBlockHeaderExtraDataAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FIllExtraData_TransactionPackingDisabled()
        {
            var merkleTreeRoot = HashHelper.ComputeFrom("MerkleTreeRoot");
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot};
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 2
            };
            _crossChainTestHelper.AddFakeExtraData(header.PreviousBlockHash, expected);
            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(new BlockIndex
            {
                BlockHash = header.PreviousBlockHash,
                BlockHeight = header.Height - 1
            }, false);
            var bytes = await _crossChainBlockExtraDataProvider.GetBlockHeaderExtraDataAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData_Test()
        {
            var merkleTreeRoot = HashHelper.ComputeFrom("MerkleTreeRoot");
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot};
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 2
            };
            _crossChainTestHelper.AddFakeExtraData(header.PreviousBlockHash, expected);
            var bytes = await _crossChainBlockExtraDataProvider.GetBlockHeaderExtraDataAsync(header);
            Assert.Equal(expected.ToByteString(), bytes);
        }
    }
}