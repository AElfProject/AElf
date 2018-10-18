using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class BlockTest
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainService _chainService;

        public BlockTest(IChainService chainService, IChainCreationService chainCreationService)
        {
            //_smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainService = chainService;
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

            var chainId = Hash.Generate();
            return await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});
        }
     
       [Fact]
        public void GenesisBlockBuilderTest()
        {
            var builder = new GenesisBlockBuilder().Build(Hash.Generate());
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
        
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
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