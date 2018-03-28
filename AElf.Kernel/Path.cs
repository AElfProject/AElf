using System;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class Path : IPath
    {
        private bool IsPointer { get; set; }

        private IHash _chainHash;
        private IHash _blockHash;
        private IHash _accountAddress;
        private string _itemName = "";

        public Path SetChainHash(IHash chainHash)
        {
            _chainHash = chainHash;
            return this;
        }
        
        public Path SetBlockHash(IHash blockHash)
        {
            _blockHash = blockHash;
            IsPointer = true;
            return this;
        }
        
        public Path SetAccount(IHash accountAddress)
        {
            _accountAddress = accountAddress;
            return this;
        }

        public Path SetItemName(string itemName)
        {
            _itemName = itemName;
            return this;
        }

        public IHash GetPointerHash()
        {
            throw new NotImplementedException();
            /*
            if (!PointerValidation())
            {
                throw new InvalidOperationException("Invalide pointer.");
            }

            return new Hash(this.CalculateHash());*/
        }

        public IHash GetPathHash()
        {
            throw new NotImplementedException();
            /*
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalide path.");
            }

            return new Hash(this.CalculateHash());*/
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