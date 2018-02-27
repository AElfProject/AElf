using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        
        private readonly WorldState _worldState;
        private bool _isInitialized;
        private readonly GenesisBlock _genesisBlock;

        public Chain(WorldState worldState, GenesisBlock genesisBlock)
        {
            _worldState = worldState;
            _genesisBlock = genesisBlock;
        }

        /// <summary>
        /// A memory based block storage
        /// </summary>
        /// <value>The blocks.</value>
        public List<IBlock> Blocks { get; set; } = new List<IBlock>();

        /// <summary>
        /// AccountZero 
        /// </summary>
        public AccountZero AccountZero { get;  private set; }
        
        
        /// <summary>
        /// AccountManager
        /// </summary>
        public AccountManager AccountManager { get;  private set; }
        
        /// <summary>
        /// Inititalize for accountZero
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            if(_isInitialized)
                return false;
            _isInitialized = true;
            
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
                var smartContractZero = new SmartContractZero();
                AccountZero = new AccountZero(smartContractZero);
                
                // create accountDataProvider for accountZero
                _worldState.AddAccountDataProvider(AccountZero);
                var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(AccountZero);
                
                // create SmartContractMap dataProvider accountZeroDataProvider
                const string smartContractMapKey = "SmartContractMap";
                accountZeroDataProvider.GetDataProvider().SetDataProvider(smartContractMapKey,
                    new DataProvider(_worldState, Hash<IAccount>.Zero));
                
                // create SmartContractRegistration for accountZero
                await smartContractZero.InititalizeAsync(accountZeroDataProvider);
                var transaction = _genesisBlock.Transaction;
                var smartContractRegistration =
                    new SmartContractRegistration
                    {
                        Category = (int) transaction.Params[0],
                        Name = (string) transaction.Params[1],
                        Bytes = (byte[]) transaction.Params[2],
                        Hash = new Hash<SmartContractRegistration>(
                            Hash<IAccount>.Zero.CalculateHashWith((string) transaction.Params.ElementAt(1)))
                    };
            
                // register contracts on accountZero
                await smartContractZero.RegisterSmartContract(smartContractRegistration);
                
                // create accountManager
                AccountManager = new AccountManager(_worldState, AccountZero);
            });
        }
        
        
        public long CurrentBlockHeight => Blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHeader().GetTransactionMerkleTreeRoot().Value);
    }
}