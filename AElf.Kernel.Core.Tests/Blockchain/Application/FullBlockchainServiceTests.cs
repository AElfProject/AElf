using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests : AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ILocalEventBus _localEventBus;

        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task Create_Chain_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();

            await _fullBlockchainService.CreateChainAsync(block);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
        }

        [Fact]
        public async Task Add_Block_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader(),
                Body = new BlockBody()
            };

            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.Header.GetHash());
            existBlock.ShouldBeNull();

            await _fullBlockchainService.AddBlockAsync(block);
            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.Header.GetHash());

            existBlock.ShouldNotBeNull();
        }

        [Fact]
        public async Task Has_Block_ReturnTrue()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.HasBlockAsync(blockList[1].GetHash());
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Has_Block_ReturnFalse()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.HasBlockAsync(Hash.FromString("Not Exist Block"));
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnNull()
        {
            var chain = await CreateNewChain();
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 2);
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
        public async Task Get_GetReservedBlockHashes_ReturnNull()
        {
            var result = await _fullBlockchainService.GetReversedBlockHashes(Hash.FromString("not exist"), 1);
            result.ShouldBeNull();
        }


        [Fact]
        public async Task Get_GetBlockHashes_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var result = await _fullBlockchainService.GetBlockHashes(chain, Hash.FromString("not exist"), 1);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnHashes()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetReversedBlockHashes(blockList[2].GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(blockList[1].GetHash());
            result[1].ShouldBe(blockList[0].GetHash());
        }

        [Fact]
        public async Task Get_GetBlockHashes_ReturnHashes()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetBlockHashes(chain, blockList[0].GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(blockList[1].GetHash()); //6c56
            result[1].ShouldBe(blockList[2].GetHash()); //

            result = await _fullBlockchainService.GetBlockHashes(chain, blockList[1].GetHash(), 1);
            result.Count.ShouldBe(1);
            result[0].ShouldBe(blockList[2].GetHash()); //
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnBlock()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetBlockByHeightAsync(3);
            result.ShouldNotBeNull();
            result.GetHash().ShouldBe(blockList[1].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnNull()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetBlockByHeightAsync(5);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnBlock()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetBlockByHashAsync(blockList[2].GetHash());
            result.ShouldNotBeNull();
            result.Height.ShouldBe(blockList[2].Height);
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnNull()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

            var result = await _fullBlockchainService.GetBlockByHashAsync(Hash.FromString("Not Exist Block"));
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnChain()
        {
            await CreateNewChain();

            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();
        }


        [Fact]
        public async Task Get_BestChain_ReturnBlockHeader()
        {
            var (chain, blockList) = await CreateNewChainWithBlock(3);

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

            await _fullBlockchainService.AddBlockAsync(newBlock);
            chain = await _fullBlockchainService.GetChainAsync();
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);

            var result = await _fullBlockchainService.GetBestChainLastBlock();
            result.Height.ShouldBe(blockList[2].Height);
            result.GetHash().ShouldBe(blockList[2].GetHash());
        }

        private async Task<Chain> CreateNewChain()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            if (chain != null)
            {
                throw new InvalidOperationException();
            }

            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            chain = await _fullBlockchainService.CreateChainAsync(genesisBlock);
            return chain;
        }

        private async Task<(Chain, List<Block>)> CreateNewChainWithBlock(int blockCount)
        {
            var chain = await CreateNewChain();
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

                await _fullBlockchainService.AddBlockAsync(newBlock);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
                await _fullBlockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());
            }

            return (chain, blockList);
        }
    }
}