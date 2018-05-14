using System;
using System.Linq;
using AElf.Kernel.Extensions;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class Path : IPath
    {
        public bool IsPointer { get; private set; }

        private Hash _chainHash;
        private Hash _blockHash;
        private Hash _accountAddress;
        private Hash _dataProviderHash;
        private Hash _keyHash;

        public Path SetChainHash(Hash chainHash)
        {
            _chainHash = chainHash;
            return this;
        }
        
        public Path SetBlockHash(Hash blockHash)
        {
            _blockHash = blockHash;
            IsPointer = true;
            return this;
        }

        /// <summary>
        /// Change a pointer to a path.
        /// </summary>
        /// <returns></returns>
        public Path SetBlockHashToNull()
        {
            _blockHash = null;
            IsPointer = false;
            return this;
        }
        
        public Path SetAccount(Hash accountAddress)
        {
            _accountAddress = accountAddress;
            return this;
        }

        public Path SetDataProvider(Hash dataProviderHash)
        {
            _dataProviderHash = dataProviderHash;
            return this;
        }

        public Path SetDataKey(Hash keyHash)
        {
            _keyHash = keyHash;
            return this;
        }
            
        public Hash GetPointerHash()
        {
            if (!PointerValidation())
            {
                throw new InvalidOperationException("Invalide pointer.");
            }

            return CalculateListHash(_chainHash, _accountAddress, _dataProviderHash, _keyHash, _blockHash);
        }

        public Hash GetPathHash()
        {
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalide path.");
            }

            return CalculateListHash(_chainHash, _accountAddress, _dataProviderHash, _keyHash);
        }
        
        private bool PointerValidation()
        {
            return _chainHash != null && _blockHash != null && _accountAddress != null && _dataProviderHash != null &&
                   _keyHash != null;
        }

        private bool PathValidation()
        {
            return _chainHash != null && _accountAddress != null && _dataProviderHash != null && _keyHash != null;
        }

        private Hash CalculateListHash(params Hash[] hashes)
        {
            if (hashes.Length == 1)
            {
                return hashes[0];
            }
            var remains = hashes.Skip(1).ToArray();
            return hashes[0].CombineHashWith(CalculateListHash(remains));
        }
    }
}