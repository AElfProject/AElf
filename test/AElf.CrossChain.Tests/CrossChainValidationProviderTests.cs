using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Xunit;
using AElf.CSharp.Core.Extension;

namespace AElf.CrossChain
{
    public sealed class CrossChainValidationProviderTest : CrossChainTestBase
    {
        private readonly IBlockValidationProvider _crossChainBlockValidationProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public CrossChainValidationProviderTest()
        {
            _crossChainBlockValidationProvider = GetRequiredService<IBlockValidationProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }

        [Fact]
        public async Task Validate_GenesisHeight_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1
                }
            };
            var validationRes = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(validationRes);
        }

        [Fact]
        public async Task Validate_EmptyHeader_NoIndexedData_Test()
        {
            var block = _kernelTestHelper.GenerateBlock(10, Hash.Empty);
            block.Header.Bloom = ByteString.CopyFrom(new byte[256]);
            var validationRes = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(validationRes);
        }

        [Fact]
        public async Task Validate_EmptyHeader_WithIndexedData_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
            var fakeSideChainId = ChainHelper.ConvertBase58ToChainId("2112");
            var fakeSideChainBlockData = CreateSideChainBlockData(fakeSideChainId, 1, fakeMerkleTreeRoot1);

            CreateFakeStateData(fakeSideChainId, fakeSideChainBlockData, 2);
            var block = _kernelTestHelper.GenerateBlock(1, Hash.Empty);
            var bloom = new Bloom();
            bloom.Combine(new[]
            {
                GetSideChainBlockDataIndexedEventBloom()
            });
            block.Header.Bloom = ByteString.CopyFrom(bloom.Data);

            var res = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.False(res);
        }

        [Fact]
        public async Task Validate_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
            int chainId = ChainHelper.ConvertBase58ToChainId("2112");
            var sideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1,
                ChainId = chainId
            };
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new[] {sideChainBlockData});
            var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
            block.Header.Bloom = ByteString.CopyFrom(GetSideChainBlockDataIndexedEventBloom().Data);
            
            var fakeIndexedCrossChainData = new CrossChainBlockData();
            fakeIndexedCrossChainData.SideChainBlockDataList.Add(sideChainBlockData);
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(2, fakeIndexedCrossChainData);
        
            var res = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.True(res);
        }
        
        [Fact]
        public async Task Validate_IncompatibleExtraData_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
            var fakeSideChainId = ChainHelper.ConvertBase58ToChainId("2112");
            var fakeSideChainBlockData = CreateSideChainBlockData(fakeSideChainId, 1, fakeMerkleTreeRoot1);
            CreateFakeStateData(fakeSideChainId, fakeSideChainBlockData, 2);
        
            // mock data in cache
            var fakeTxnMerkleTreeRoot = HashHelper.ComputeFrom("fakeMerkleTreeRoot2");
        
            var block = CreateFilledBlock(fakeTxnMerkleTreeRoot);
            block.Header.Bloom = ByteString.CopyFrom(GetSideChainBlockDataIndexedEventBloom().Data);
        
            var res = await _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateBlockBeforeExecute_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
            var fakeSideChainId = ChainHelper.ConvertBase58ToChainId("2112");
            var fakeSideChainBlockData = CreateSideChainBlockData(fakeSideChainId, 1, fakeMerkleTreeRoot1);
            var sideChainTxMerkleTreeRoot = ComputeRootHash(new[] {fakeSideChainBlockData});
            var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
            {
                var res = await _crossChainBlockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
                Assert.False(res);
            }
            
            {
                _crossChainTestHelper.AddFakeExtraData(block.Header.PreviousBlockHash,
                    new CrossChainExtraData {TransactionStatusMerkleTreeRoot = sideChainTxMerkleTreeRoot});
                
                var res = await _crossChainBlockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
                Assert.True(res);
            }
        }
        
        [Fact]
        public async Task ValidateBlockBeforeAttach_Test()
        {
            {
                var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
                var fakeSideChainId = ChainHelper.ConvertBase58ToChainId("2112");
                var fakeSideChainBlockData = CreateSideChainBlockData(fakeSideChainId, 1, fakeMerkleTreeRoot1);
                var sideChainTxMerkleTreeRoot = ComputeRootHash(new[] {fakeSideChainBlockData});
                var block = CreateFilledBlock(sideChainTxMerkleTreeRoot);
                var res = await _crossChainBlockValidationProvider.ValidateBeforeAttachAsync(block);
                Assert.True(res);
            }
        }
        
        private IBlock CreateFilledBlock(Hash merkleTreeRoot)
        {
            var block = _kernelTestHelper.GenerateBlock(1, Hash.Empty);
            block.Header.ExtraData.Clear();
            block.Header.ExtraData.Add(CrossChainConstants.CrossChainExtraDataKey,
                new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot}.ToByteString());
            return block;
        }
        
        
        private Hash ComputeRootHash(IEnumerable<SideChainBlockData> blockInfo)
        {
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(blockInfo.Select(sideChainBlockData =>
                sideChainBlockData.TransactionStatusMerkleTreeRoot));
            return binaryMerkleTree.Root;
        }
        
        private void CreateFakeStateData(int fakeSideChainId, SideChainBlockData fakeSideChainBlockData,
            long height = 1)
        {
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            // mock data in state
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(height, fakeIndexedCrossChainBlockData);
        }

        private SideChainBlockData CreateSideChainBlockData(int chainId, long height, Hash transactionMerkleTreeRoot)
        {
            return new SideChainBlockData
            {
                Height = height,
                TransactionStatusMerkleTreeRoot = transactionMerkleTreeRoot,
                ChainId = chainId
            };
        }

        private Bloom GetSideChainBlockDataIndexedEventBloom()
        {
            var logEvent = new SideChainBlockDataIndexed().ToLogEvent();
            return logEvent.GetBloom();
        }
    }
}