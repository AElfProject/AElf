using System.Threading;
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
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public ChainTest(ISmartContractZero smartContractZero, IChainCreationService chainCreationService,
            IChainManager chainManager, IWorldStateStore worldStateStore, 
            IChangesStore changesStore, IDataStore dataStore)
        {
            _smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        [Fact]
        public async Task<IChain> CreateChainTest()
        {
            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, _smartContractZero.GetType());

            /*// add chain to storage
            var genesisBlock = new Block(Hash.Zero);
            await _chainManager.AddChainAsync(chain.Id, genesisBlock.GetHash());
            
            var block = new Block(genesisBlock.GetHash()) {Header = {PreviousHash = genesisBlock.GetHash()}};
            await _chainManager.AppendBlockToChainAsync(chain.Id, block);
            
            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, 
                _changesStore, _dataStore).OfChain(chainId);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            
            await _smartContractZero.InitializeAsync(accountDataProvider);*/
            Assert.Equal(await _chainManager.GetChainCurrentHeight(chain.Id), (ulong)1);
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
            await _chainManager.AddChainAsync(chainId, Hash.Generate());
            Assert.NotNull(_chainManager.GetChainAsync(chainId).Result);
        }
        

        [Fact]
        public async Task AppendBlockTest()
        {
            var chain = await CreateChainTest();

            var block = CreateBlock(chain.GenesisBlockHash);
            await _chainManager.AppendBlockToChainAsync(chain.Id, block);
            Assert.Equal(await _chainManager.GetChainCurrentHeight(chain.Id), (ulong)2);
            Assert.Equal(await _chainManager.GetChainLastBlockHash(chain.Id), block.GetHash());
            Assert.Equal(block.Header.Index, (ulong)1);
        }
        
        private Block CreateBlock(Hash preBlockHash = null)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousHash = preBlockHash;
            return block;
        }
    }
}