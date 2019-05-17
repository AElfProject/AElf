using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.AElfConsensus.Tests
{
    public class ConsensusValidationProviderTests: DPoSConsensusTestBase
    {
        private readonly IBlockValidationProvider _blockValidationProvider;

        public ConsensusValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<IBlockValidationProvider>();
        }

        [Fact]
        public async Task Validate_Block_BeforeExecuteAsync()
        {
            var block1 = GenerateBlockInformation(1, true);
            var result1 = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block1);
            result1.ShouldBeTrue();

            var block2 = GenerateBlockInformation(10, false);
            var result2 = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block2);
            result2.ShouldBeFalse();
            
            var block3 = GenerateBlockInformation(10, true);
            var result3 = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block3);
            result3.ShouldBeTrue();                
        }

        [Fact]
        public async Task Validate_Block_AfterExecuteAsync()
        {
            var block1 = GenerateBlockInformation(1, true);
            var result1 = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block1);
            result1.ShouldBeTrue();

            var block2 = GenerateBlockInformation(10, false);
            var result2 = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block2);
            result2.ShouldBeFalse();
            
            var block3 = GenerateBlockInformation(10, true);
            var result3 = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block3);
            result3.ShouldBeTrue();
        }

        private Block GenerateBlockInformation(long height, bool withBlockExtraData)
        {
            var header = new BlockHeader(){ Height = height, PreviousBlockHash = Hash.Generate() };
            if (withBlockExtraData)
                header.BlockExtraDatas.Add(ByteString.CopyFromUtf8("test1"));
                
            var body = new BlockBody
            {
                BlockHeader = header.GetHash(),
                Transactions = { Hash.Generate() }
            };
            
            return new Block
            {
                Height = header.Height,
                Header = header,
                Body = body
            };
       }
    }
}