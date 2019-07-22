using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusValidationProviderTests : ConsensusTestBase
    {
        private readonly IBlockValidationProvider _blockValidationProvider;

        public ConsensusValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<IBlockValidationProvider>();
        }

        [Fact]
        public async Task ValidateBeforeAttachAsync_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1,
                    ExtraData = { }
                }
            }; 
            var result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeTrue();

            block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = 10010,
                    Height = 10,
                    SignerPubkey = ByteString.CopyFromUtf8("pubkey"),
                    Signature = ByteString.CopyFromUtf8("sig data"),
                    PreviousBlockHash = Hash.Generate(),
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    MerkleTreeRootOfWorldState = Hash.Generate(),
                    MerkleTreeRootOfTransactionStatus = Hash.Generate()
                }
            };
            result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeFalse();
            
            block.Header = new BlockHeader
            {
                Height = 10,
                ExtraData = { ByteString.CopyFromUtf8("test") }
            };
            result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockBeforeExecuteAsync_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1,
                    ExtraData = { }
                }
            };
            var result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();
            
            block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = 10010,
                    Height = 10,
                    SignerPubkey = ByteString.CopyFromUtf8("pubkey"),
                    Signature = ByteString.CopyFromUtf8("sig data"),
                    PreviousBlockHash = Hash.Generate(),
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    MerkleTreeRootOfWorldState = Hash.Generate(),
                    MerkleTreeRootOfTransactionStatus = Hash.Generate()
                }
            };
            result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeFalse();
            
            block.Header = new BlockHeader
            {
                ChainId = 10010,
                Height = 10,
                SignerPubkey = ByteString.CopyFromUtf8("pubkey"),
                Signature = ByteString.CopyFromUtf8("sig data"),
                PreviousBlockHash = Hash.Generate(),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactions = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate(),
                MerkleTreeRootOfTransactionStatus = Hash.Generate(),
                ExtraData =
                {
                    ByteString.CopyFromUtf8("extra data")
                }
            };
            result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockAfterExecuteAsync_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1,
                    ExtraData = { }
                }
            };
            var result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();
            
            block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = 10010,
                    Height = 10,
                    SignerPubkey = ByteString.CopyFromUtf8("pubkey"),
                    Signature = ByteString.CopyFromUtf8("sig data"),
                    PreviousBlockHash = Hash.Generate(),
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    MerkleTreeRootOfWorldState = Hash.Generate(),
                    MerkleTreeRootOfTransactionStatus = Hash.Generate()
                }
            };
            result = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            result.ShouldBeFalse();
            
            block.Header = new BlockHeader
            {
                ChainId = 10010,
                Height = 10,
                SignerPubkey = ByteString.CopyFromUtf8("pubkey"),
                Signature = ByteString.CopyFromUtf8("sig data"),
                PreviousBlockHash = Hash.Generate(),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactions = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate(),
                MerkleTreeRootOfTransactionStatus = Hash.Generate(),
                ExtraData =
                {
                    ByteString.CopyFromUtf8("extra data")
                }
            };
            result = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            result.ShouldBeTrue();
        }
    }
}