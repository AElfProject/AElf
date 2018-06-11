using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainTest
    {
        private readonly IChainCreationService _chainCreationService;
        //private readonly ISmartContractZero _smartContractZero;
        private readonly IChainManager _chainManager;
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public ChainTest(IChainCreationService chainCreationService,
            IChainManager chainManager, IWorldStateStore worldStateStore, 
            IChangesStore changesStore, IDataStore dataStore)
        {
            //_smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.SmartContractZero/bin/Debug/netstandard2.0/AElf.Contracts.SmartContractZero.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        [Fact]
        public async Task<IChain> CreateChainTest()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);

            // add chain to storage
            
            //var address = Hash.Generate();
            //var worldStateManager = await new WorldStateManager(_worldStateStore, 
            //    _changesStore, _dataStore).OfChain(chainId);
            //var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            
            //await _smartContractZero.InitializeAsync(accountDataProvider);
            Assert.Equal(await _chainManager.GetChainCurrentHeight(chain.Id), (ulong)1);
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