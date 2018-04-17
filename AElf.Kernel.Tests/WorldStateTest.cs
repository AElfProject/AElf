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
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerCollection _pointerCollection;
        private readonly IChainStore _chainStore;
        private readonly IChangesCollection _changesCollection;

        public WorldStateTest(IChainStore chainStore, IWorldStateStore worldStateStore, 
            IPointerCollection pointerCollection, IChangesCollection changesCollection)
        {
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _pointerCollection = pointerCollection;
            _changesCollection = changesCollection;
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
                accountContextService, _pointerCollection, _changesCollection);

            var worldState = await worldStateManager.GetWorldStateAsync(chain.Id);
            
            Assert.NotNull(worldState);
            Assert.NotNull(worldState.GetWorldStateMerkleTreeRootAsync());
        }
    }
}