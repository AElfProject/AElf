using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests : AElfKernelTestBase
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
            var eventMessage = new BestChainFoundEventData();
            _localEventBus.Subscribe<BestChainFoundEventData>(message =>
            {
                eventMessage = message;
                return Task.CompletedTask;
            });

            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                    ChainId = _chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            var chain = await _fullBlockchainService.GetChainAsync(_chainId);
            chain.ShouldBeNull();

            await _fullBlockchainService.CreateChainAsync(_chainId, block);

            chain = await _fullBlockchainService.GetChainAsync(_chainId);
            chain.ShouldNotBeNull();

            eventMessage.BlockHeight.ShouldBe(ChainConsts.GenesisBlockHeight);
        }

        [Fact]
        public async Task Add_Block_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader(),
                Body = new BlockBody()
            };

            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(_chainId, block.Header.GetHash());
            existBlock.ShouldBeNull();

            await _fullBlockchainService.AddBlockAsync(_chainId, block);
            existBlock = await _fullBlockchainService.GetBlockByHashAsync(_chainId, block.Header.GetHash());

            existBlock.ShouldNotBeNull();
        }

        [Fact]
        public async Task Has_Block_ReturnTrue()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.HasBlockAsync(_chainId, blockList[1].GetHash());
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Has_Block_ReturnFalse()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.HasBlockAsync(_chainId, Hash.FromString("Not Exist Block"));
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnNull()
        {
            var chain = await CreateNewChain(_chainId);
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 2ul);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnHash()
        {
            var chain = await CreateNewChain(_chainId);
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
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.GetBlockHeaders(chain.Id, blockList[2].GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(blockList[1].GetHash());
            result[1].ShouldBe(blockList[0].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnBlock()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.GetBlockByHeightAsync(_chainId, 3);
            result.ShouldNotBeNull();
            result.GetHash().ShouldBe(blockList[1].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnNull()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.GetBlockByHeightAsync(_chainId, 5);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnBlock()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.GetBlockByHashAsync(_chainId, blockList[2].GetHash());
            result.ShouldNotBeNull();
            result.Height.ShouldBe(blockList[2].Height);
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnNull()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var result = await _fullBlockchainService.GetBlockByHashAsync(_chainId, Hash.FromString("Not Exist Block"));
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnChain()
        {
            await CreateNewChain(_chainId);

            var chain = await _fullBlockchainService.GetChainAsync(_chainId);
            chain.ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync(_chainId);
            chain.ShouldBeNull();
        }


        [Fact]
        public async Task Get_BestChain_ReturnBlockHeader()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(_chainId, 3);

            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = Hash.FromString("New Branch")
                },
                Body = new BlockBody()
            };
            blockList.Add(newBlock);

            await _fullBlockchainService.AddBlockAsync(_chainId, newBlock);
            chain = await _fullBlockchainService.GetChainAsync(_chainId);
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);

            var result = await _fullBlockchainService.GetBestChainLastBlock(_chainId);
            result.Height.ShouldBe(blockList[2].Height);
            result.GetHash().ShouldBe(blockList[2].GetHash());
        }

        private async Task<Chain> CreateNewChain(int chainId)
        {
            var chain = await _fullBlockchainService.GetChainAsync(chainId);
            if (chain != null)
            {
                throw new InvalidOperationException();
            }

            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis
                },
                Body = new BlockBody()
            };
            chain = await _fullBlockchainService.CreateChainAsync(chainId, genesisBlock);
            return chain;
        }

        private async Task<(Chain, List<Block>)> CreateNewChainWithBlock(int chainId, int blockCount)
        {
            var chain = await CreateNewChain(chainId);
            var blockList = new List<Block>();

            for (var i = 0; i < blockCount; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = chain.BestChainHeight + 1,
                        PreviousBlockHash = chain.BestChainHash
                    },
                    Body = new BlockBody()
                };
                blockList.Add(newBlock);

                await _fullBlockchainService.AddBlockAsync(chainId, newBlock);
                chain = await _fullBlockchainService.GetChainAsync(chainId);
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            }

            return (chain, blockList);
        }
    }
}