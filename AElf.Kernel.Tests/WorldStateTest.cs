using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Common.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.BlockSyncTests;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly BlockTest _blockTest;
        private readonly ITxPoolService _txPoolService;
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly ITransactionStore _transactionStore;
        private readonly IChainService _chainService;
        private readonly IChainManagerBasic _chainManager;
        private readonly IBlockManagerBasic _blockManger;
        private readonly ICanonicalHashStore _canonicalHashStore;

        public WorldStateTest(IChainStore chainStore, IWorldStateStore worldStateStore,
            IChangesStore changesStore, IDataStore dataStore, BlockTest blockTest, ILogger logger,
            ITxPoolService txPoolService, IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            ITransactionStore transactionStore, IChainService chainService, IChainManagerBasic chainManager, IBlockManagerBasic blockManager,
            ICanonicalHashStore canonicalHashStore)
        {
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
            _blockTest = blockTest;
            _logger = logger;
            _txPoolService = txPoolService;
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            _transactionStore = transactionStore;
            _chainService = chainService;
            _chainManager = chainManager;
            _blockManger = blockManager;
            _canonicalHashStore = canonicalHashStore;
        }

        [Fact]
        public async Task GetWorldStateTest()
        {
            // Data preparation
            var chain = await _blockTest.CreateChain();
            var worldStateDictator = 
                new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                    _blockHeaderStore, _blockBodyStore, _transactionStore,  _logger).SetChainId(chain.Id);

            var blockchain = _chainService.GetBlockChain(chain.Id);
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            
            //because the hash value of block1 will be changed in appending operation, 
            //so reverse this two operations.
            await AddBlockAsync(blockchain, worldStateDictator, block1);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await AddBlockAsync(blockchain, worldStateDictator, block2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash());

            var worldState = await worldStateDictator.GetWorldStateAsync(block1.GetHash());
            
            Assert.NotNull(worldState);
        }

        private async Task AddBlockAsync(IBlockChain blockchain, IWorldStateDictator worldStateDictator, IBlock block)
        {
            await blockchain.AddBlocksAsync(new List<IBlock>(){ block });
        }
        
        [Fact]
        public async Task GetHistoryWorldStateRootTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var worldStateDictator = 
                new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                    _blockHeaderStore, _blockBodyStore, _transactionStore,  _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();//Just fake one

            var blockchain = _chainService.GetBlockChain(chain.Id);
            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);
            
            var key = new Hash("testkey".CalculateHash());
            
            var address = Hash.Generate();
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(key, data1);
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(key, data2);
            var data3= Hash.Generate().Value.ToArray();
            var subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(key, data3);
            var data4 = Hash.Generate().Value.ToArray();
            var subDataProvider4 = dataProvider.GetDataProvider("test4");
            await subDataProvider4.SetAsync(key, data4);
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await AddBlockAsync(blockchain, worldStateDictator, block1);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().Value.ToArray();
            subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(key, data5);
            var data6 = Hash.Generate().Value.ToArray();
            subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(key, data6);
            var data7= Hash.Generate().Value.ToArray();
            subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(key, data7);
            
            var changes1 = await worldStateDictator.GetChangesAsync(chain.GenesisBlockHash);
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash()); 
            await AddBlockAsync(blockchain, worldStateDictator, block2);
            
            //Test the continuity of changes (through two sequence world states).
            var getChanges1 = await worldStateDictator.GetChangesAsync(chain.GenesisBlockHash);
            var changes2 = await worldStateDictator.GetChangesAsync(block1.GetHash());
            Assert.True(changes1.Count == getChanges1.Count);
            Assert.True(changes1[0].After == getChanges1[0].After);
            Assert.True(changes1[3].After == getChanges1[3].After);
            Assert.True(changes1.Select(c => c.After).
                            Intersect(changes2.Select(c => c.Befores.FirstOrDefault())).Count() == 3);

            //Test the equality of pointer transfered from path and get from world state.
            var path = new ResourcePath()
                .SetChainId(chain.Id)
                .SetAccountAddress(address)
                .SetDataProvider(subDataProvider1.GetHash())
                .SetDataKey(key);
            var pointerHash1 = path.SetBlockProducerAddress(worldStateDictator.BlockProducerAccountAddress)
                .SetBlockHash(chain.GenesisBlockHash).GetPointerHash();
            var pointerHash2 = path.SetBlockProducerAddress(worldStateDictator.BlockProducerAccountAddress)
                .SetBlockHash(block1.GetHash()).GetPointerHash();
            Assert.True(changes2[0].GetLastHashBefore() == pointerHash1);
            Assert.True(changes2[0].After == pointerHash2);

            //Test data equal or not equal from different world states.
            var getData1InHeight1 = await subDataProvider1.GetAsync(key, chain.GenesisBlockHash);
            var getData1InHeight2 = await subDataProvider1.GetAsync(key, block1.GetHash());
            var getData1 = await subDataProvider1.GetAsync(key);
            Assert.True(data1.SequenceEqual(getData1InHeight1));
            Assert.False(data1.SequenceEqual(getData1InHeight2));
            Assert.True(data5.SequenceEqual(getData1InHeight2));
            Assert.True(getData1InHeight2.SequenceEqual(getData1));

            var block3 = CreateBlock(block2.GetHash(), chain.Id, 3);
            await AddBlockAsync(blockchain, worldStateDictator, block3);
            await worldStateDictator.SetWorldStateAsync(block3.GetHash());

            var changes3 = await worldStateDictator.GetChangesAsync();
            
            Assert.True(changes3.Count == 0);

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data8 = Hash.Generate().Value.ToArray();
            var subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data8);
            
            var block4 = CreateBlock(block3.GetHash(), chain.Id, 4);
            await AddBlockAsync(blockchain, worldStateDictator, block4);
            await worldStateDictator.SetWorldStateAsync(block4.GetHash());

            var changes4 = await worldStateDictator.GetChangesAsync(block3.GetHash());
            
            Assert.True(changes4.Count == 1);
            var getData8 = await subDataProvider5.GetAsync(key);
            Assert.True(data8.SequenceEqual(getData8));
            
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data9 = Hash.Generate().Value.ToArray();
            subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data9);
            
            await worldStateDictator.RollbackCurrentChangesAsync();
            worldStateDictator.PreBlockHash = ((BlockHeader)await blockchain.GetHeaderByHashAsync(worldStateDictator.PreBlockHash)).PreviousBlockHash;
            
            var getData9 = await subDataProvider5.GetAsync(key);
            Assert.False(data9.SequenceEqual(getData9));
            Assert.True(data8.SequenceEqual(getData9));
        }

        [Fact]
        public async Task RollbackCurrentChangesTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                    _blockHeaderStore, _blockBodyStore, _transactionStore,  _logger)
                .SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();//Just fake one
            var blockchain = _chainService.GetBlockChain(chain.Id);
            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);
            
            var address = Hash.Generate();
            
            var key1 = new Hash("testkey1".CalculateHash());
            var key2 = new Hash("testkey2".CalculateHash());

            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data1);
            await subDataProvider.SetAsync(key2, data2);
            
            await AddBlockAsync(blockchain, worldStateDictator, block1);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data3 = Hash.Generate().Value.ToArray();
            var data4 = Hash.Generate().Value.ToArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data3);
            await subDataProvider.SetAsync(key2, data4);

            var getData3 = await subDataProvider.GetAsync(key1);
            Assert.Equal(data3, getData3);
            var getData4 = await subDataProvider.GetAsync(key2);
            Assert.Equal(data4, getData4);

            //Do the rollback
            await worldStateDictator.RollbackCurrentChangesAsync();
            worldStateDictator.PreBlockHash = ((BlockHeader)await blockchain.GetHeaderByHashAsync(worldStateDictator.PreBlockHash)).PreviousBlockHash;

            //Now the "key"'s value of subDataProvider rollback to previous data.
            var getData1 = await subDataProvider.GetAsync(key1);
            var getData2 = await subDataProvider.GetAsync(key2);
            Assert.NotEqual(data3, getData1);
            Assert.NotEqual(data4, getData2);
            Assert.Equal(data1, getData1);
            Assert.Equal(data2, getData2);
            
            //Set again
            await subDataProvider.SetAsync(key1, data3);
            await subDataProvider.SetAsync(key2, data4);
            
            Assert.Equal(data3, await subDataProvider.GetAsync(key1));
            Assert.Equal(data4, await subDataProvider.GetAsync(key2));

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block2);

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().Value.ToArray();
            var data6 = Hash.Generate().Value.ToArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data6);
            await subDataProvider.SetAsync(key1, data5);

            var getData5 = await subDataProvider.GetAsync(key1);
            Assert.Equal(data5, getData5);

            await worldStateDictator.RollbackCurrentChangesAsync();
            worldStateDictator.PreBlockHash = ((BlockHeader)await blockchain.GetHeaderByHashAsync(worldStateDictator.PreBlockHash)).PreviousBlockHash;
            
            getData3 = await subDataProvider.GetAsync(key1);
            Assert.Equal(data3, getData3);
        }

        [Fact]
        public async Task RollbackToSpecificHeightTest()
        {
            ulong index = 0;
            
            var chain = await _blockTest.CreateChain();
            System.Diagnostics.Debug.WriteLine($"Hash of height 0: {chain.GenesisBlockHash.Value.ToByteArray().ToHex()}");
            
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore,  _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();//Just fake one

            var blockchain = _chainService.GetBlockChain(chain.Id);
            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);
            
            var key = new Hash("testkey".CalculateHash());
            
            var address = Hash.Generate();
            
            //----------------- height 1 -----------------
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().ToByteArray();
            var subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data1);
            
            //--------------- set height 1 ---------------
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, ++index);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block1);

            //----------------- height 2 -----------------
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data2 = Hash.Generate().ToByteArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data2);
            
            //--------------- set height 2 ---------------
            var block2 = CreateBlock(block1.GetHash(), chain.Id, ++index);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block2);

            //----------------- height 3 -----------------
            //Though do nothing
            
            //Check value from height 2
            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var indx = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
                return indx + 1;
            });
            Assert.Equal("3", (await getNextHeight()).ToString());
            Assert.Equal(data2, await subDataProvider.GetAsync(key));
            
            //Do rollback - rollback world state to height 1
            await blockchain.RollbackToHeight(0);
            await worldStateDictator.RollbackToBlockHash(chain.GenesisBlockHash);
//            await worldStateDictator.RollbackToSpecificHeight(1);
            
            //Reset the index
            index = 0;
            //Check result of rollback
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider("test");
            Assert.True(1 == await getNextHeight());
            Assert.Equal(data1, await subDataProvider.GetAsync(key));

            //----------------- height 1 -----------------(again)
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data3 = Hash.Generate().ToByteArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data3); // value: data1 -> data3
            
            //--------------- set height 1 ---------------
            var block1Quote = CreateBlock(chain.GenesisBlockHash, chain.Id, ++index);
            await worldStateDictator.SetWorldStateAsync(block1Quote.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block1Quote);

            //----------------- height 2 -----------------
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data4 = Hash.Generate().ToByteArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data4);
            
            //--------------- set height 2 ---------------
            var block2Quote = CreateBlock(block1Quote.GetHash(), chain.Id, ++index);

            await worldStateDictator.SetWorldStateAsync(block2Quote.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block2Quote);
            
            //----------------- height 3 -----------------
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().ToByteArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data5);
            
            //--------------- set height 3 ---------------
            var block3Quote = CreateBlock(block2Quote.GetHash(), chain.Id, ++index);
            await worldStateDictator.SetWorldStateAsync(block3Quote.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block3Quote);

            //----------------- height 4 -----------------
            //Though do nothing

            //Check the value of latest height
            Assert.Equal(data5, await subDataProvider.GetAsync(key));
            
            //Check height before rollback
            Assert.Equal("4", (await getNextHeight()).ToString());

            //Let's rollback to height 2
            
            await blockchain.RollbackToHeight(block1Quote.Header.Index);
            await worldStateDictator.RollbackToBlockHash(block1Quote.GetHash());
//            await worldStateDictator.RollbackToSpecificHeight(2);
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider("test");
            //And check
            Assert.Equal("2", (await getNextHeight()).ToString());
            Assert.Equal(data4, await subDataProvider.GetAsync(key));
        }

        [Fact]
        public async Task CheckoutTest()
        {
            var chain = await _blockTest.CreateChain();
            System.Diagnostics.Debug.WriteLine($"Hash of height 0: {chain.GenesisBlockHash.Value.ToByteArray().ToHex()}");
            
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore,  _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();//Just fake one

            var blockchain = _chainService.GetBlockChain(chain.Id);

            var key = new Hash("testkey".CalculateHash());
            
            var address = Hash.Generate();
            
            //----------------- height 1 -----------------
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().ToByteArray();
            var subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data1);
            
            //--------------- set height 1 ---------------
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block1);

            //----------------- height 2 -----------------
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data2 = Hash.Generate().ToByteArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key, data2);
            
            //--------------- set height 2 ---------------
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block2);
            
            //------------ create height 2 again------------
            var block2Again = CreateBlock(block1.GetHash(), chain.Id, 2);
            //Assuming the validation filter result is ValidationError.Orphan, and passed the consensus validation
//            await worldStateDictator.RollbackToSpecificHeight(block2Again.Header.Index);
            await blockchain.RollbackToHeight(block1.Header.Index);
            await worldStateDictator.RollbackToBlockHash(block1.Header.GetHash());

            var getNextHeight = new Func<Task<ulong>>(async () =>
            {
                var curHash = await blockchain.GetCurrentBlockHashAsync();
                var indx = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
                return indx + 1;
            });
            
            Assert.Equal("2", (await getNextHeight()).ToString());
            
            //--------------- set height 2 ---------------
            await worldStateDictator.SetWorldStateAsync(block2Again.GetHash());
            await AddBlockAsync(blockchain, worldStateDictator, block2Again);

            Assert.Equal("3", (await getNextHeight()).ToString());
        }

        /// <summary>
        /// the hash of block created by this method will be changed when appending to a chain.
        /// (basically change the block header's Index value)
        /// </summary>
        /// <param name="preBlockHash"></param>
        /// <param name="chainId"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Index = index;
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();
            block.Body.BlockHeader = block.Header.GetHash();

            System.Diagnostics.Debug.WriteLine($"Hash of height {index}: {block.GetHash().Value.ToByteArray().ToHex()}\twith previous hash {preBlockHash.Value.ToByteArray().ToHex()}");

            return block;
        }
    }
}