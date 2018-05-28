using System;
using System.Linq;
using AElf.Kernel.Extensions;
using Google.Protobuf.WellKnownTypes;
using NLog.Layouts;

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
                throw new InvalidOperationException("Invalid pointer.");
            }

            return CalculateListHash(_chainHash, _accountAddress, _dataProviderHash, _keyHash, _blockHash);
        }

        public Hash GetPathHash()
        {
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalid path.");
            }

            return CalculateListHash(_chainHash, _accountAddress, _dataProviderHash, _keyHash);
        }
        
        #region Calculate pointer for chain context

        public static Hash CalculatePointerForCurrentBlockHeight(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "Height".CalculateHash());
        }

        public static Hash CalculatePointerForLastBlockHash(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "LastBlockHash".CalculateHash());
        }
        
        /// <summary>
        /// calculate hash for account zero in a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForAccountZero(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "AccountZero".CalculateHash());
        }
        
        #endregion

        
        /// <summary>
        /// calculate new account address
        /// using parent account addr and nonce
        /// </summary>
        /// <param name="parentAccount"></param>
        /// <param name="nonce"></param>
        /// <returns></returns>
        public static Hash CalculateAccountAddress(Hash parentAccount, ulong nonce)
        {
            return parentAccount.CalculateHashWith(new UInt64Value
            {
                Value = nonce
            });
        }
        
        
        #region Calculate pointer for block context

        public static Hash CalculatePointerForPathsCount(Hash chainId, Hash blockHash)
        {
            Hash foo = chainId.CalculateHashWith(blockHash);
            return foo.CalculateHashWith((Hash) "PathsCount".CalculateHash());
        }
        
        #endregion

        #region Calculate pointer for tx result

        public static Hash CalculatePointerForTxResult(Hash txId)
        {
            return txId.CalculateHashWith((Hash)"Result".CalculateHash());
        }

        #endregion
        
        #region Private methods
        
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
        
        #endregion
    }
}