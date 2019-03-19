using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class FullBlockchainExecutingServiceValidateFailedTests : ValidateAfterFailedTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainManager _chainManager;
        
        public FullBlockchainExecutingServiceValidateFailedTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _chainManager = GetRequiredService<IChainManager>();
        }
        
        [Fact]
        public async Task ExecuteBlocksAttachedToLongestChain_ValidateFailed()
        {
            var chain = await CreateNewChain();

            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                },
                Body = new BlockBody()
            };
            
            await _blockchainService.AddBlockAsync(newBlock);
            var status = await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
            
            attachResult.Count.ShouldBe(2);
            attachResult.Last().Height.ShouldBe(2);
            attachResult.Last().BlockHash.ShouldBe(newBlock.GetHash());

            chain = await _blockchainService.GetChainAsync();
            var newBlockLink = await _chainManager.GetChainBlockLinkAsync(newBlock.GetHash());
            
            newBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionFailed);
            chain.BestChainHash.ShouldBe(chain.GenesisBlockHash);
            chain.BestChainHeight.ShouldBe(ChainConsts.GenesisBlockHeight);
        }
        
        private async Task<Chain> CreateNewChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            
            var chain = await _blockchainService.CreateChainAsync( genesisBlock);
            return chain;
        }
    }
}