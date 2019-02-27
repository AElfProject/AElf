using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainExecutingServiceTests: AElfKernelTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IFullBlockchainService _fullBlockchainService;
        private readonly ILocalEventBus _localEventBus;
        private readonly int _chainId = 1;
        
        public FullBlockchainExecutingServiceTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _fullBlockchainService = GetRequiredService<IFullBlockchainService>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }
        
        [Fact]
        public async Task Attach_Block_To_Chain_ReturnNull()
        {
            var eventMessage = new BestChainFoundEventData();
            _localEventBus.Subscribe<BestChainFoundEventData>(message =>
            {
                eventMessage = message;
                return Task.CompletedTask;
            });
            
            var chain = await CreateNewChain();
            
            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = 2,
                    PreviousBlockHash = Hash.Zero
                },
                Body = new BlockBody()
            };
            
            var attachResult = await _fullBlockchainExecutingService.AttachBlockToChainAsync(chain, newBlock);
            attachResult.ShouldBeNull();
            eventMessage.BlockHeight.ShouldBe(ChainConsts.GenesisBlockHeight);
        }
        
        [Fact]
        public async Task Attach_Block_To_Chain_FoundBestChain()
        {
            var eventMessage = new BestChainFoundEventData();
            _localEventBus.Subscribe<BestChainFoundEventData>(message =>
            {
                eventMessage = message;
                return Task.CompletedTask;
            });
            
            var chain = await CreateNewChain();

            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = chain.LongestChainHeight + 1,
                    PreviousBlockHash = chain.LongestChainHash
                },
                Body = new BlockBody()
            };

            await _fullBlockchainService.AddBlockAsync(chain.Id, newBlock);
            var attachResult = await _fullBlockchainExecutingService.AttachBlockToChainAsync(chain, newBlock);
            attachResult.Count.ShouldBe(2);
            eventMessage.BlockHeight.ShouldBe(newBlock.Header.Height);
        }
        
        private async Task<Chain> CreateNewChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis
                },
                Body = new BlockBody()
            };
            var chain = await _fullBlockchainService.CreateChainAsync(_chainId, genesisBlock);
            return chain;
        }
    }
}