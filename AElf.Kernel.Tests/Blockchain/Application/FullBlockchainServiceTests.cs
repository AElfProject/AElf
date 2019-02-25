using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests: AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ILocalEventBus _localEventBus;
        private readonly int _chainId = 1;
        
        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
            
        }

        [Fact]
        public async Task Create_Chain_Success()
        {
            var eventMessage = new BestChainFoundEvent();
            _localEventBus.Subscribe<BestChainFoundEvent>(message =>
            {
                eventMessage = message;
                return Task.CompletedTask;
            });

            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = GlobalConfig.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                    ChainId = _chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };
            await _fullBlockchainService.CreateChainAsync(_chainId, block);

            var chain = await _fullBlockchainService.GetChainAsync(_chainId);
            chain.Id.ShouldBe(_chainId);

            eventMessage.BlockHeight.ShouldBe(GlobalConfig.GenesisBlockHeight);
        }

        [Fact]
        public async Task Add_Block_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader(), 
                Body = new BlockBody()
            };

            await _fullBlockchainService.AddBlockAsync(_chainId, block);
            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(_chainId, block.Header.GetHash());
            
            existBlock.ShouldNotBeNull();
        }

        [Fact]
        public async Task Attach_Block_To_Chain_ReturnNull()
        {
            var eventMessage = new BestChainFoundEvent();
            _localEventBus.Subscribe<BestChainFoundEvent>(message =>
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

            var attachResult = await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            attachResult.ShouldBeNull();
            eventMessage.BlockHeight.ShouldBe(GlobalConfig.GenesisBlockHeight);
        }
        
        [Fact]
        public async Task Attach_Block_To_Chain_FoundBestChain()
        {
            var eventMessage = new BestChainFoundEvent();
            _localEventBus.Subscribe<BestChainFoundEvent>(message =>
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
            var attachResult = await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            attachResult.Count.ShouldBe(2);
            eventMessage.BlockHeight.ShouldBe(newBlock.Header.Height);
        }
        
        [Fact]
        public async Task Attach_Block_To_Chain_NotFoundBestChain()
        {
            var eventMessage = new BestChainFoundEvent();
            _localEventBus.Subscribe<BestChainFoundEvent>(message =>
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
            chain.BestChainHeight = 3;

            await _fullBlockchainService.AddBlockAsync(chain.Id, newBlock);
            var attachResult = await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            attachResult.Count.ShouldBe(2);
            eventMessage.BlockHeight.ShouldBe(1ul);
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnNull()
        {
            var chain = await CreateNewChain();
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 2ul);
            result.ShouldBeNull();
        }
        
        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnHash()
        {
            var chain = await CreateNewChain();
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, chain.BestChainHeight);
            result.ShouldBe(chain.BestChainHash);
        }

        [Fact]
        public async Task Get_BlockHeaders_ReturnNull()
        {
            var result = await _fullBlockchainService.GetBlockHeaders(_chainId, Hash.FromString("not exist"), 1);
            result.ShouldBeNull();
        }
        
        [Fact]
        public async Task Get_BlockHeaders_ReturnHeaders()
        {
            var chain = await CreateNewChain();
            var newBlock1 = new Block
            {
                Header = new BlockHeader
                {
                    Height = chain.LongestChainHeight + 1,
                    PreviousBlockHash = chain.LongestChainHash
                },
                Body = new BlockBody()
            };
            await _fullBlockchainService.AddBlockAsync(_chainId, newBlock1);
            chain = await _fullBlockchainService.GetChainAsync(_chainId);
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock1);
            
            var newBlock2 = new Block
            {
                Header = new BlockHeader
                {
                    Height = newBlock1.Height + 1,
                    PreviousBlockHash = newBlock1.GetHash()
                },
                Body = new BlockBody()
            };
            await _fullBlockchainService.AddBlockAsync(_chainId, newBlock2);
            chain = await _fullBlockchainService.GetChainAsync(_chainId);
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock2);
            
            var newBlock3 = new Block
            {
                Header = new BlockHeader
                {
                    Height = newBlock2.Height + 1,
                    PreviousBlockHash = newBlock2.GetHash()
                },
                Body = new BlockBody()
            };
            await _fullBlockchainService.AddBlockAsync(_chainId, newBlock3);
            chain = await _fullBlockchainService.GetChainAsync(_chainId);
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock3);

            var result = await _fullBlockchainService.GetBlockHeaders(_chainId, newBlock3.GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(newBlock2.GetHash());
            result[1].ShouldBe(newBlock1.GetHash());
        }

        private async Task<Chain> CreateNewChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = GlobalConfig.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis
                },
                Body = new BlockBody()
            };
            var chain = await _fullBlockchainService.CreateChainAsync(_chainId, genesisBlock);
            return chain;
        }
    }
}