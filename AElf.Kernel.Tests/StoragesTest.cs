using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerStore _pointerStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;

        public StoragesTest(IChainStore chainStore, 
            IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            IWorldStateStore worldStateStore, IPointerStore pointerStore, IChangesStore changesStore)
        {
            _chainStore = chainStore;
            
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
            _changesStore = changesStore;
        }

        [Fact]
        public async Task ChainStoreTest()
        {
            var chainId = Hash.Generate();
            var chain = new Chain(chainId);
            var chainManager = new ChainManager(_chainStore);
            
            await chainManager.AddChainAsync(chain.Id);

            var blockHash = Hash.Generate();
            var block = new Block(blockHash);

            await chainManager.AppendBlockToChainAsync(chain, block);

            var getChain = await chainManager.GetChainAsync(chainId);
            
            Assert.True(chain.CurrentBlockHash == getChain.CurrentBlockHash);
            Assert.True(chainId == chain.Id);
            Assert.True(chain.Id == getChain.Id);

            await chainManager.AppendBlockToChainAsync(chain, new Block(Hash.Generate()));
            
            Assert.True(chain.CurrentBlockHeight == 2);
        }

        [Fact]
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();

            var blockHeaderStore = _blockHeaderStore;
            var blockBodyStore = _blockBodyStore;
            
            await blockHeaderStore.InsertAsync(block.Header);
            await blockBodyStore.InsertAsync(block.Header.MerkleTreeRootOfTransactions, block.Body);

            //block hash = block header hash
            var blockHeaderHash = block.Header.GetHash();
            var blockHash = block.GetHash();
            Assert.True(blockHash == blockHeaderHash);
            
            var getBlock = await blockHeaderStore.GetAsync(blockHeaderHash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
            
            //block body hash = transactions merkle tree root hash
            var blockBodyHash = block.Header.MerkleTreeRootOfTransactions;
            var getBlockBody = await blockBodyStore.GetAsync(blockBodyHash);
            
            Assert.True(Equals(block.Body, getBlockBody));
        }
        
        [Fact]
        public async Task OneBlockDataProviderTest()
        {
            #region Prepare data
            //Create a chain with one block.
            var chain = new Chain();
            var chainManager = new ChainManager(_chainStore);
            var blockHash = Hash.Generate();
            var block = new Block(blockHash);
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            await chainManager.AppendBlockToChainAsync(chain, block);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, blockHash, 
                accountContextService, _pointerStore, _changesStore);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            #endregion
            
            //Get the DataProvider of the AccountDataProvider.
            var dataProvider = accountDataProvider.GetDataProvider();

            //Set data to the DataProvider and get it.
            var data = new byte[] {1, 1, 1, 1};
            await dataProvider.SetAsync(data);
            var getData = await dataProvider.GetAsync();
            
            Assert.True(data.SequenceEqual(getData));

            //Get a sub-DataProvider from aforementioned DataProvider.
            var subDataProvider = dataProvider.GetDataProvider("test");

            //Same as before.
            var data2 = new byte[] {1, 2, 3, 4};

            await subDataProvider.SetAsync(data2);
            var getData2 = await subDataProvider.GetAsync();
            
            Assert.True(data2.SequenceEqual(getData2));

            var data3 = new byte[] {4, 3, 2, 1};

            await subDataProvider.SetAsync(data3);
            var getData3 = await subDataProvider.GetAsync();
            
            Assert.True(data3.SequenceEqual(getData3));
        }

        [Fact]
        public async Task TwoBlockDataProviderTest()
        {
            #region Prepare data
            //Create a chain with one block.
            var chain = new Chain(Hash.Generate());
            var chainManager = new ChainManager(_chainStore);
            var block1 = new Block(Hash.Generate());
            var block2 = new Block(Hash.Generate());
            block1.AddTransaction(Hash.Generate());
            block1.AddTransaction(Hash.Generate());
            block1.FillTxsMerkleTreeRootInHeader();
            block2.AddTransaction(Hash.Generate());
            block2.AddTransaction(Hash.Generate());
            block2.FillTxsMerkleTreeRootInHeader();
            
            await chainManager.AddChainAsync(chain.Id);
            await chainManager.AppendBlockToChainAsync(chain, block1);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, block1.GetHash(), 
                accountContextService, _pointerStore, _changesStore);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            #endregion

            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider("test");
            var data = new byte[] {1, 2, 3, 4};
            await subDataProvider.SetAsync(data);
            
            Assert.True(chain.CurrentBlockHeight == 1);
            
            var getDataFromHeight1 = await subDataProvider.GetAsync();
            
            Assert.True(data.SequenceEqual(getDataFromHeight1));

            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block2.GetHash());
            await chainManager.AppendBlockToChainAsync(chain, block2);
            
            Assert.True(chain.CurrentBlockHeight == 2);

            accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider("test");
            
            var getDataFromHeight2 = await subDataProvider.GetAsync();

            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var data2 = new byte[] {1, 2, 3, 4, 5};
            await subDataProvider.SetAsync(data2);
            getDataFromHeight2 = await subDataProvider.GetAsync();
            
            Assert.False(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var getDataFromHeight1ByBlockHash = await subDataProvider.GetAsync(block1.GetHash());
            
            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight1ByBlockHash));
        }
    }
}