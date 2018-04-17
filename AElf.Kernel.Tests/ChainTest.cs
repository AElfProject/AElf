using System;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
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
        private readonly IPointerCollection _pointerCollection;
        private readonly IChangesCollection _changesCollection;

        public ChainTest(ISmartContractZero smartContractZero, IChainCreationService chainCreationService,
            IChainManager chainManager, IChainContextService chainContextService, IWorldStateStore worldStateStore, IPointerCollection pointerCollection, IChangesCollection changesCollection)
        {
            _smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _chainContextService = chainContextService;
            _worldStateStore = worldStateStore;
            _pointerCollection = pointerCollection;
            _changesCollection = changesCollection;
        }

        [Fact]
        public async Task<Chain> CreateChainTest()
        {
            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, _smartContractZero.GetType());

            await _chainManager.AppendBlockToChainAsync(chain, new Block(Hash.Generate()));
            
            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, Hash.Generate(), 
                accountContextService, _pointerCollection, _changesCollection);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chainId, address);
            
            await _smartContractZero.InitializeAsync(accountDataProvider);
            Assert.Equal(chain.CurrentBlockHeight, (ulong)1);
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

        public async Task AppendBlockTest(Chain chain, Block block)
        {
            await _chainManager.AppendBlockToChainAsync(chain, block);
            Assert.Equal(chain.CurrentBlockHeight, (ulong)2);
            Assert.Equal(chain.CurrentBlockHash, block.GetHash());
        }
    }
}