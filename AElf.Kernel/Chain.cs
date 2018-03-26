using System;
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
            CurrentBlockHash = genesisBlock.GetHash();
            CurrentBlockHeight = 0;
            Id = new Hash<IChain>(genesisBlock.GetHash().Value);
        }

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
            throw new NotImplementedException();
            /*Task.Factory.StartNew(async () =>
            {
                var transaction = _genesisBlock.Transaction;
                var smartContractRegistration =
                    new SmartContractRegistration
                    {
                        Category = (int) transaction.Params[0],
                        Name = (string) transaction.Params[1],
                        Bytes = (byte[]) transaction.Params[2]
                    };
            
                // register contracts on accountZero
                var smartContractZero = new SmartContractZero();
                _accountZero = new AccountZero(smartContractZero);
                var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(_accountZero);
                await smartContractZero.InititalizeAsync(accountZeroDataProvider);
                await smartContractZero.RegisterSmartContract(smartContractRegistration);
                
            }).Wait();*/
            
        }


        public long CurrentBlockHeight { get; private set; }
        public IHash<IBlock> CurrentBlockHash { get; private set; }
        public void UpdateCurrentBlock(IBlock block)
        {
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }

        public IHash<IChain> Id { get; private set; }
        public IHash<IBlock> GenesisBlockHash { get; }
    }
}