using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private readonly IBlockHeaderStore _blockStore;

        private readonly IWorldStateStore _worldStateStore;

        private readonly IPointerStore _pointerStore;
        
        private readonly IChainStore _chainStore;

        private readonly Dictionary<Hash, IChangesStore> _changesList;
        
        public StoragesTest(IBlockHeaderStore blockStore, IChainStore chainStore, 
            IWorldStateStore worldStateStore, IPointerStore pointerStore, Dictionary<Hash, IChangesStore> changesList)
        {
            _blockStore = blockStore;
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
            _changesList = changesList;
        }
        
        [Fact]
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            await _blockStore.InsertAsync(block.Header);

            var hash = block.GetHash();
            var getBlock = await _blockStore.GetAsync(hash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
        }

        [Fact]
        public async void ChainStoreTest()
        {
            var chain = new Chain();
            
            //Prepare
            var preBlockHash = Hash.Generate();
            var preBlock = new Block(preBlockHash);
            preBlock.AddTransaction(Hash.Generate());
            chain.UpdateCurrentBlock(preBlock);
            
            var chainManager = new ChainManager(_chainStore);
            await chainManager.AppendBlockToChainAsync(chain, preBlock);
        }

        [Fact]
        public async void AccountDataChangeTest()
        {
            #region Generate a chain with one block
            
            var chain = new Chain();
            
            var preBlockHash = Hash.Generate();
            var preBlock = new Block(preBlockHash);
            preBlock.AddTransaction(Hash.Generate());
            chain.UpdateCurrentBlock(preBlock);
            
            #endregion

            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, preBlockHash, 
                accountContextService, _pointerStore, _changesList);
            var worldState = worldStateManager.GetWorldStateAsync(chain.Id);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
        }
        
    }
}