using System;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class Path : IPath
    {
        private bool IsPointer { get; set; }

        private IHash<IChain> _chainHash;
        private IHash<IBlock> _blockHash;
        private IHash<IAccount> _accountAddress;
        private string _itemName = "";

        public Path SetChainHash(IHash<IChain> chainHash)
        {
            _chainHash = chainHash;
            return this;
        }
        
        public Path SetBlockHash(IHash<IBlock> blockHash)
        {
            _blockHash = blockHash;
            IsPointer = true;
            return this;
        }
        
        public Path SetAccount(IHash<IAccount> accountAddress)
        {
            _accountAddress = accountAddress;
            return this;
        }

        public Path SetItemName(string itemName)
        {
            _itemName = itemName;
            return this;
        }

        public IHash<IPath> GetPointerHash()
        {
            if (!PointerValidation())
            {
                throw new InvalidOperationException("Invalide pointer.");
            }

            return new Hash<IPath>(this.CalculateHash());
        }

        public IHash<IPath> GetPathHash()
        {
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalide path.");
            }

            return new Hash<IPath>(this.CalculateHash());
        }

        private bool PointerValidation()
        {
            return _chainHash != null && _blockHash != null && _accountAddress != null && _itemName != "";
        }

        private bool PathValidation()
        {
            return !IsPointer && _chainHash != null && _accountAddress != null && _itemName != "";
        }
    }
}