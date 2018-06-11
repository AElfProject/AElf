using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Xunit;
using Xunit.Frameworks.Autofac;
using ServiceStack;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class BlockTest
    {
        private readonly IBlockManager _blockManager;
        private readonly ChainTest _chainTest;
        private readonly IChainCreationService _chainCreationService;

        public BlockTest(IBlockManager blockManager, ChainTest chainTest, IChainCreationService chainCreationService)
        {
            _blockManager = blockManager;
            //_smartContractZero = smartContractZero;
            _chainTest = chainTest;
            _chainCreationService = chainCreationService;
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.SmartContractZero/bin/Debug/netstandard2.0/AElf.Contracts.SmartContractZero.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
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
            return await _chainCreationService.CreateNewChainAsync(chainId, reg);
        }
     
       [Fact]
        public void GenesisBlockBuilderTest()
        {
            var builder = new GenesisBlockBuilder().Build();
            var genesisBlock = builder.Block;
            //var txs = builder.Txs;
            Assert.NotNull(genesisBlock);
            Assert.Equal(genesisBlock.Header.PreviousHash, Hash.Zero);
            //Assert.NotNull(txs);
        }

        [Fact]
        public async Task BlockManagerTest()
        {
            var chain = await CreateChain();

            var block = CreateBlock(chain.GenesisBlockHash);
            await _blockManager.AddBlockAsync(block);
            var b = await _blockManager.GetBlockAsync(block.GetHash());
            Assert.Equal(b, block);
        }
        
        private Block CreateBlock(Hash preBlockHash = null)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousHash = preBlockHash;
            return block;
        }
        
    }
}