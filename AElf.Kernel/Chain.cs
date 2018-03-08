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
        private readonly AccountZero _accountZero;
        private readonly IWorldState _worldState;
        private readonly GenesisBlock _genesisBlock;
        private readonly IChainContext _chainContext;
        private readonly IChainManager _chainManager;

        public Chain(AccountZero accountZero, IWorldState worldState, GenesisBlock genesisBlock, 
            IChainManager chainManager, IChainContext chainContext)
        {
            _accountZero = accountZero;
            _worldState = worldState;
            _genesisBlock = genesisBlock;
            _chainContext = chainContext;
            _chainManager = chainManager;
            CurrentBlockHash = genesisBlock.GetHash();
            GenesisBlockHash = genesisBlock.GetHash();
            CurrentBlockHeight = 0;
            Id = new Hash<IChain>(genesisBlock.GetHash().Value);
        }

        private bool _isInitialized;

        
        /// <summary>
        /// initialize chain with a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        private bool Initialize(ITransaction transaction)
        {
            // if initialized, return false
            if(_isInitialized)
                return false;
            _isInitialized = true;
            
            // deploy AccountZero with transaction
            return _chainContext.InitializeChain(this, _accountZero, transaction);
        }
        
        

        public long CurrentBlockHeight { get; private set; }
        public IHash<IBlock> CurrentBlockHash { get; private set; }
        public void UpdateCurrentBlock(IBlock block)
        {
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }

        public IHash<IChain> Id { get; }
        public IHash<IBlock> GenesisBlockHash { get; }
    }
}