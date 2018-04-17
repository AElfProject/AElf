using System.ComponentModel.DataAnnotations;
using System.Linq;
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

        [Fact]
        public async Task GetHistoryWorldStateRootTest()
        {
            var chain = new Chain(Hash.Generate());
            var block1 = CreateBlock();
            var chainManger = new ChainManager(_chainStore);
            await chainManger.AddChainAsync(chain.Id);

            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, block1.GetHash(), 
                accountContextService, _pointerCollection, _changesCollection);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            var dataProvider = accountDataProvider.GetDataProvider();
            
            var data1 = Hash.Generate().Value.ToArray();
            var subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(data1);
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(data2);
            var data3= Hash.Generate().Value.ToArray();
            var subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(data3);
            var data4 = Hash.Generate().Value.ToArray();
            var subDataProvider4 = dataProvider.GetDataProvider("test4");
            await subDataProvider4.SetAsync(data4);
            
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block1.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block1);

            var worldState1 = await worldStateManager.GetWorldStateAsync(chain.Id);
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().Value.ToArray();
            subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(data5);
            var data6 = Hash.Generate().Value.ToArray();
            subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(data6);
            var data7= Hash.Generate().Value.ToArray();
            subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(data7);
            var data8 = Hash.Generate().Value.ToArray();
            subDataProvider4 = dataProvider.GetDataProvider("test4");
            await subDataProvider4.SetAsync(data8);
            
            var block2 = CreateBlock();
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block2.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block2);

            var worldState1Again = await worldStateManager.GetWorldStateAsync(chain.Id, block1.GetHash());
            var worldState2 = await worldStateManager.GetWorldStateAsync(chain.Id);

            var changes1 = await worldState1.GetChangesAsync();
            var getChanges1 = await worldState1Again.GetChangesAsync();
            var changes2 = await worldState2.GetChangesAsync();
            Assert.True(changes1.Count == getChanges1.Count);
            Assert.True(changes1[0].After == getChanges1[0].After);
            Assert.True(changes1[3].After == getChanges1[3].After);
            Assert.True(changes1[0].After == changes2[0].Before);
            Assert.True(changes1[1].After == changes2[1].Before);
            Assert.True(changes1[2].After == changes2[2].Before);
            Assert.True(changes1[3].After == changes2[3].Before);
        }
        
        private Block CreateBlock()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }
    }
}