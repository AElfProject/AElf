using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Common;
using Shouldly;

namespace AElf.Kernel.Tests
{
    public class BlockTest: AElfKernelTestBase
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainService _chainService;

        public BlockTest()
        {
            _chainCreationService = GetRequiredService<IChainCreationService>();
            _chainService = GetRequiredService<IChainService>();
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;
        
        [Fact]
        public async Task GetBlockByHeightTest()
        {
            var chain = await CreateChain();
            var blockchain = _chainService.GetBlockChain(chain.Id);

            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, GlobalConfig.GenesisBlockHeight + 1);
            await blockchain.AddBlocksAsync(new List<IBlock> {block1});
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, GlobalConfig.GenesisBlockHeight + 2);
            await blockchain.AddBlocksAsync(new List<IBlock> {block2});

            var blockOfHeight1 = await blockchain.GetBlockByHeightAsync(GlobalConfig.GenesisBlockHeight + 1);
            Assert.Equal(block1, blockOfHeight1);

            var blockOfHeight2 = await blockchain.GetBlockByHeightAsync(GlobalConfig.GenesisBlockHeight + 2);
            Assert.Equal(block2, blockOfHeight2);

        }

        public async Task<IChain> CreateChain()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = GlobalConfig.DefaultChainId.ConvertBase58ToChainId();
            return await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});
        }
     
       [Fact]
        public void GenesisBlockBuilderTest()
        {
            var builder = new GenesisBlockBuilder().Build(ChainHelpers.GetRandomChainId());
            var genesisBlock = builder.Block;
            //var txs = builder.Txs;
            Assert.NotNull(genesisBlock);
            Assert.Equal(genesisBlock.Header.PreviousBlockHash, Hash.Genesis);
            //Assert.NotNull(txs);
        }

        [Fact]
        public async Task BlockManagerTest()
        {
            var chain = await CreateChain();

            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, GlobalConfig.GenesisBlockHeight + 1);
            var blockchain = _chainService.GetBlockChain(chain.Id);
            await blockchain.AddBlocksAsync(new List<IBlock>() {block});
            Console.WriteLine("getting " + block.GetHash());
            var b = await blockchain.GetBlockByHashAsync(block.GetHash());
//            await _blockManager.AddBlockAsync(block);
//            var b = await _blockManager.GetBlockAsync(block.GetHash());
            Assert.Equal(b, block);
        }

        [Fact]
        public async Task Test()
        {
            var chain = await CreateChain();

            var blocks = new List<Block>();
            var preBlockHash = chain.GenesisBlockHash;
            for (ulong i = 2; i < 610; i++)
            {
                var newBlock = CreateBlock(preBlockHash, chain.Id, i);
                blocks.Add(newBlock);
                preBlockHash = newBlock.GetHash();
            }

            var blockchain = _chainService.GetBlockChain(chain.Id);

            await blockchain.AddBlocksAsync(blocks);

            var block389 = await blockchain.GetBlockByHashAsync(blocks.First(b => b.Index == 389).GetHash());
            var block605 = await blockchain.GetBlockByHashAsync(blocks.First(b => b.Index == 605).GetHash());


        }
        
        private Block CreateBlock(Hash preBlockHash, int chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Genesis, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(FakeTransaction());
            block.AddTransaction(FakeTransaction());
            block.AddTransaction(FakeTransaction());
            block.AddTransaction(FakeTransaction());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Index = index;
            block.Header.MerkleTreeRootOfWorldState = Hash.Default;

            block.Body.BlockHeader = block.Header.GetHash();

            
            return block;
        }

        private Transaction FakeTransaction()
        {
            return new Transaction
            {
                From = Address.Generate(),
                To = Address.Generate()
            };
        }
    }
}