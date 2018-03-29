using System;
using System.Linq;
using AElf.Kernel.Extensions;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class Path : IPath
    {
        public bool IsPointer { get; private set; }

        private Hash _chainHash;
        private Hash _blockHash;
        private Hash _accountAddress;
        private Hash _dataProviderHash;

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

        public IHash GetPointerHash()
        {
            if (!PointerValidation())
            {
                throw new InvalidOperationException("Invalide pointer.");
            }

            return CalculateListHash(_chainHash, _blockHash, _accountAddress, _dataProviderHash);
        }

        public IHash GetPathHash()
        {
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalide path.");
            }

            return CalculateListHash(_chainHash, _accountAddress, _dataProviderHash);
        }

        private bool PointerValidation()
        {
            return _chainHash != null && _blockHash != null && _accountAddress != null && _dataProviderHash != null;
        }

        private bool PathValidation()
        {
            return !IsPointer && _chainHash != null && _accountAddress != null && _dataProviderHash != null;
        }

        private Hash CalculateListHash(params Hash[] hashes)
        {
            if (hashes.Length == 1)
            {
                return hashes[0];
            }
            var remains = hashes.Skip(1).ToArray();
            return new Hash(hashes[0].CalculateHashWith(CalculateListHash(remains)));
        }
    }
}