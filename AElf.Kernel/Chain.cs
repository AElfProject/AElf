using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        private AccountZero _accountZero;
        private readonly IWorldState _worldState;
        private bool _isInitialized;
        private readonly GenesisBlock _genesisBlock;

        public Chain(AccountZero accountZero, IWorldState worldState, GenesisBlock genesisBlock)
        {
            _accountZero = accountZero;
            _worldState = worldState;
            _genesisBlock = genesisBlock;
        }

        /// <summary>
        /// A memory based block storage
        /// </summary>
        /// <value>The blocks.</value>
        public List<Block> Blocks { get; set; } = new List<Block>();

        
        /// <summary>
        /// Inititalize for accountZero
        /// </summary>
        /// <returns></returns>
        private bool Initialize()
        {
            if(_isInitialized)
                return false;
            _isInitialized = true;
            
            // delply accountZero
            DeployContractInAccountZero();
            
            // TODO: add genesis to chain
            return true;
            
        }
        
        
        /// <summary>
        /// deploy contracts for AccountZero
        /// </summary>
        private void DeployContractInAccountZero()
        {
            Task.Factory.StartNew(async () =>
            {
                var transaction = _genesisBlock.Transaction;
                var smartContractRegistration =
                    new SmartContractRegistration
                    {
                        Category = (int) transaction.Params.ElementAt(0),
                        Name = (string) transaction.Params.ElementAt(1),
                        Bytes = (byte[]) transaction.Params.ElementAt(2)
                    };
            
                // register contracts on accountZero
                var smartContractZero = new SmartContractZero();
                _accountZero = new AccountZero(smartContractZero);
                var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(_accountZero);
                await smartContractZero.InititalizeAsync(accountZeroDataProvider);
                await smartContractZero.RegisterSmartContract(smartContractRegistration);
                
            }).Wait();
            
        }
        
        
        public long CurrentBlockHeight => Blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHeader().GetTransactionMerkleTreeRoot().Value);
    }
}