using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainTest
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly ISmartContractZero _smartContractZero;
        private readonly IChainManager _chainManager;
        private readonly IChainContextService _chainContextService;
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public ChainTest(ISmartContractZero smartContractZero, IChainCreationService chainCreationService,
            IChainManager chainManager, IChainContextService chainContextService, IWorldStateStore worldStateStore, 
            IChangesStore changesStore, IDataStore dataStore)
        {
            _smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _chainContextService = chainContextService;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        [Fact]
        public async Task<IChain> CreateChainTest()
        {
            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, _smartContractZero.GetType());

            await _chainManager.AppendBlockToChainAsync(chain, new Block(Hash.Generate()));
            
            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, 
                accountContextService, _changesStore, _dataStore);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chainId, address);
            
            await _smartContractZero.InitializeAsync(accountDataProvider);
            Assert.Equal((ulong)2, chain.CurrentBlockHeight);
            return chain;
        }

        [Fact]
        public async Task<IChain> CreateMultipleChainTest()
        {
            var chainId = Hash.Generate();
            var chainId2 = Hash.Generate();

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, _smartContractZero.GetType());
            var chain2 = await _chainCreationService.CreateNewChainAsync(chainId2, _smartContractZero.GetType());

            await _chainManager.AppendBlockToChainAsync(chain, new Block(Hash.Generate()));

            await _chainManager.AppendBlockToChainAsync(chain2, new Block(Hash.Generate()));
            await _chainManager.AppendBlockToChainAsync(chain2, new Block(Hash.Generate()));

            var address = Hash.Generate();
            var address2 = Hash.Generate();

            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore,
                accountContextService, _changesStore, _dataStore);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chainId, address);
            var accountDataProvider2 = worldStateManager.GetAccountDataProvider(chainId, address2);

            await _smartContractZero.InitializeAsync(accountDataProvider);
            await _smartContractZero.InitializeAsync(accountDataProvider2);

            Assert.Equal((ulong)2, chain.CurrentBlockHeight);
            Assert.Equal((ulong)3, chain2.CurrentBlockHeight);
            return chain;
        }


        public async Task ChainStoreTest(Hash chainId)
        {
            await _chainManager.AddChainAsync(chainId);
            Assert.NotNull(_chainManager.GetChainAsync(chainId).Result);
        }

        public async Task ChainContextTest()
        {
            var chain = await CreateChainTest();
            await _chainManager.AddChainAsync(chain.Id);
            chain = await _chainManager.GetChainAsync(chain.Id);
            var context = _chainContextService.GetChainContext(chain.Id);
            Assert.NotNull(context);
            Assert.Equal(context.SmartContractZero, _smartContractZero);
        }

        public async Task AppendBlockTest(IChain chain, Block block)
        {
            await _chainManager.AppendBlockToChainAsync(chain, block);
            Assert.Equal( (ulong)2, chain.CurrentBlockHeight);
            Assert.Equal(chain.CurrentBlockHash, block.GetHash());
        }
    }
}