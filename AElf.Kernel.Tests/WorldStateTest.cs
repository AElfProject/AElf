using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateTest
    {
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerStore _pointerStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;

        public WorldStateTest(IChainStore chainStore,
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
        public async Task GetWorldStateTest()
        {
            var chain = new Chain(Hash.Generate());
            var block = new Block(Hash.Generate());
            var chainManger = new ChainManager(_chainStore);

            await chainManger.AddChainAsync(chain.Id);
            await chainManger.AppendBlockToChainAsync(chain, block);
            
            var hash = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, hash, 
                accountContextService, _pointerStore, _changesStore);

            var worldState = await worldStateManager.GetWorldStateAsync(chain.Id);
            
            Assert.NotNull(worldState);
            Assert.NotNull(worldState.GetWorldStateMerkleTreeRootAsync());
        }
    }
}