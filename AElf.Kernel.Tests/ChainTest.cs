using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Managers;
using Xunit;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.TestBase;

namespace AElf.Kernel.Tests
{
    public sealed class ChainTest : AElfKernelTestBase
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainService _chainService;

        public ChainTest()
        {
            _chainCreationService = GetRequiredService<IChainCreationService>();
            _chainService = GetRequiredService<IChainService>();
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

            var chainId = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 });
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});

            var blockchain = _chainService.GetBlockChain(chainId);
            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var height = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Height;
                return height + 1;
            });
            Assert.Equal(await getNextHeight(), GlobalConfig.GenesisBlockHeight + 1);
            return chain;
        }

        [Fact]
        public async Task AppendBlockTest()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 });
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});

            var blockchain = _chainService.GetBlockChain(chainId);
            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var height = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Height;
                return height + 1;
            });
            Assert.Equal(await getNextHeight(), GlobalConfig.GenesisBlockHeight + 1);

            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, GlobalConfig.GenesisBlockHeight + 1);
            await blockchain.AddBlocksAsync(new List<IBlock> {block});
            
            Assert.Equal(await getNextHeight(), GlobalConfig.GenesisBlockHeight + 2);
            Assert.Equal((await blockchain.GetCurrentBlockHashAsync()).ToHex(), block.GetHash().ToHex());
            Assert.Equal(block.Header.Height, GlobalConfig.GenesisBlockHeight + 1);
        }
        
        private static Block CreateBlock(Hash preBlockHash, int chainId, ulong height)
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
            block.Header.Height = height;
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