using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        
        private bool _isInitialized;
        private readonly GenesisBlock _genesisBlock;
        private readonly AccountManager _accountManager ;

        public Chain(GenesisBlock genesisBlock, AccountManager accountManager)
        {
            _genesisBlock = genesisBlock;
            _accountManager = accountManager;
        }

        /// <summary>
        /// A memory based block storage
        /// </summary>
        /// <value>The blocks.</value>
        public List<IBlock> Blocks { get;} = new List<IBlock>();

        private WorldState WorldState => _accountManager.WorldState;

        private AccountZero AccountZero => _accountManager.AccountZero;

        private SmartContractZero SmartContractZero => _accountManager.AccountZero.SmartContractZero;

        /// <summary>
        /// Inititalize for accountZero
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            if(_isInitialized)
                return false;
            _isInitialized = true;

            var accountZeroDataProvider =
                new AccountDataProvider(AccountZero, WorldState);
            // create accountDataProvider for accountZero
            WorldState.AddAccountDataProvider(accountZeroDataProvider);
            
            // create SmartContractMap dataProvider in accountZeroDataProvider
            const string smartContractMapKey = "SmartContractMap";
            accountZeroDataProvider.GetDataProvider().SetDataProvider(smartContractMapKey,
                new DataProvider(WorldState, Hash<IAccount>.Zero));

            // delploy accountZero
            var task = DeployContractInAccountZero();
            task.Wait();
            
            // TODO: add genesis to chain
            Blocks.Add(_genesisBlock);
            return true;
            
        }
        
        
        /// <summary>
        /// deploy contracts for AccountZero
        /// </summary>
        private Task DeployContractInAccountZero()
        {
            return Task.Factory.StartNew(async () =>
            {
                // get smartContractZero for accountZero
                var accountZeroDataProvider = WorldState.GetAccountDataProviderByAccount(_accountManager.AccountZero);

                // inititalize
                await SmartContractZero.InitializeAsync(accountZeroDataProvider);
                
                
                var transaction = _genesisBlock.Transaction;
                var scrHash = new Hash<SmartContractRegistration>(
                    Hash<IAccount>.Zero.CalculateHashWith((string) transaction.Params.ElementAt(1)));
                var smartContractRegistration =
                    new SmartContractRegistration
                    {
                        Category = (int) transaction.Params[0],
                        Name = (string) transaction.Params[1],
                        Bytes = (byte[]) transaction.Params[2],
                        Hash = scrHash
                    };
                
                // register contracts on accountZero
                await SmartContractZero.RegisterSmartContract(smartContractRegistration);
                
            });
        }
        
        
        public long CurrentBlockHeight => Blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHeader().GetTransactionMerkleTreeRoot().Value);
    }
}