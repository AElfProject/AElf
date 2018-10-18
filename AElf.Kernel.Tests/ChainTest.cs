using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Managers;
using Xunit;
using Xunit.Frameworks.Autofac;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainTest
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainService _chainService;

        public ChainTest(IChainCreationService chainCreationService, IChainService chainService)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
        }

        private static byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        [Fact]
        public async Task<IChain> CreateChainTest()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});

            var blockchain = _chainService.GetBlockChain(chainId);
            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var indx = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
                return indx + 1;
            });
            Assert.Equal(await getNextHeight(), (ulong)1);
            return chain;
        }

//        public async Task ChainStoreTest(Hash chainId)
//        {
//            await _chainManager.AddChainAsync(chainId, Hash.Generate());
//            Assert.NotNull(_chainManager.GetChainAsync(chainId).Result);
//        }
        

        [Fact]
        public async Task AppendBlockTest()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});

            var blockchain = _chainService.GetBlockChain(chainId);
            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var indx = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
                return indx + 1;
            });
            Assert.Equal(await getNextHeight(), (ulong)1);

            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await blockchain.AddBlocksAsync(new List<IBlock> {block});
            
            Assert.Equal(await getNextHeight(), (ulong)2);
            Assert.Equal((await blockchain.GetCurrentBlockHashAsync()).DumpHex(), block.GetHash().DumpHex());
            Assert.Equal(block.Header.Index, (ulong)1);
        }
        
        private static Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
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

            return block;
        }
        
        private static Transaction FakeTransaction()
        {
            return new Transaction
            {
                From = Address.Generate(),
                To = Address.Generate()
            };
        }
    }
}