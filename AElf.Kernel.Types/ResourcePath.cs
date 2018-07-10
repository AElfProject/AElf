using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <inheritdoc />
    public class ResourcePath : IResourcePath
    {
        /// <summary>
        /// For now just change this value manually
        /// </summary>
        public bool IsPointer { get; private set; }

        private Hash _chainHash;
        private Hash _blockHash;
        private Hash _accountAddress;
        private Hash _dataProviderHash;
        private Hash _keyHash;
        private Hash _blockProducerAddress;

        public ResourcePath SetChainId(Hash chainId)
        {
            _chainHash = chainId;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }
        
        /// <inheritdoc />
        public ResourcePath SetBlockHash(Hash blockHash)
        {
            _blockHash = blockHash;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }
        
        public ResourcePath SetBlockProducerAddress(Hash blockProducerAddress)
        {
            _blockProducerAddress = blockProducerAddress;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }

        /// <summary>
        /// Basically revert a pointer to a path.
        /// </summary>
        /// <returns></returns>
        public ResourcePath RevertPointerToPath()
        {
            _blockHash = null;
            _blockProducerAddress = null;
            IsPointer = false;
            return this;
        }

        public ResourcePath SetAccountAddress(Hash accountAddress)
        {
            _accountAddress = accountAddress;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }
        
        public ResourcePath SetDataProvider(Hash dataProviderHash)
        {
            _dataProviderHash = dataProviderHash;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }
        
        public ResourcePath SetDataKey(Hash keyHash)
        {
            _keyHash = keyHash;
            if (PointerValidation())
            {
                IsPointer = true;
            }
            return this;
        }

        public Hash GetPointerHash()
        {
            if (!PointerValidation())
            {
                throw new InvalidOperationException("Invalid pointer.");
            }

            return CalculateHashOfHashList(_chainHash, _accountAddress, _dataProviderHash, _keyHash, _blockHash,
                _blockProducerAddress);
        }

        public Hash GetPathHash()
        {
            if (!PathValidation())
            {
                throw new InvalidOperationException("Invalid path.");
            }

            return CalculateHashOfHashList(_chainHash, _accountAddress, _dataProviderHash, _keyHash);
        }
        
        #region Private methods
        
        private bool PointerValidation()
        {
            return _chainHash != null && _blockHash != null && _accountAddress != null && _dataProviderHash != null &&
                   _keyHash != null && _blockProducerAddress != null;
        }

        private bool PathValidation()
        {
            return _chainHash != null && _accountAddress != null && _dataProviderHash != null && _keyHash != null;
        }

        /// <summary>
        /// Calculate hash value of a hash list one by one
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        private static Hash CalculateHashOfHashList(params Hash[] hashes)
        {
            if (hashes.Length == 1)
            {
                return hashes[0];
            }
            
            var remains = hashes.Skip(1).ToArray();
            return hashes[0].CombineHashWith(CalculateHashOfHashList(remains));
        }
        
        #endregion
        
        /*
         * Directly calculate pointer value zone
         */
        
        #region Calculate pointer for chain context

        /// <summary>
        /// Calculate pointer hash for current block height of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForCurrentBlockHeight(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "Height".CalculateHash());
        }

        /// <summary>
        /// Calculate pointer hash for last block hash of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForLastBlockHash(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "LastBlockHash".CalculateHash());
        }
        
        /// <summary>
        /// Calculate pointer hash for Account Zero of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForAccountZero(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "AccountZero".CalculateHash());
        }

        /// <summary>
        /// Calculate pointer hash for metadata template of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForMetadataTemplate(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "MetadataTemplate".CalculateHash());
        }
        
        /// <summary>
        /// Calculate pointer hash for metadata template calling graph of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForMetadataTemplateCallingGraph(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "MetadataTemplateCallingGraph".CalculateHash());
        }
        
        /// <summary>
        /// Calculate pointer hash for metadata of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForMetadata(Hash chainId)
        {
            return chainId.CalculateHashWith((Hash) "Metadata".CalculateHash());
        }

        /// <summary>
        /// Calculate pointer hash for using block height get block hash
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForGettingBlockHashByHeight(Hash chainId, ulong height)
        {
            return CalculateHashOfHashList(chainId, "HeightHashMap".CalculateHash(),
                new UInt64Value {Value = height}.CalculateHash());
        }
        
        #endregion

        #region Calculate pointer for account
        
        /// <summary>
        /// Calculate new account address,
        /// using parent account address and nonce
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
        
        #endregion
        
        #region Calculate pointer for block context

        public static Hash CalculatePointerForPathsCount(Hash chainId, Hash blockHash)
        {
            return CalculateHashOfHashList(chainId, blockHash, "PathsCount".CalculateHash());
        }
        
        #endregion

        #region Calculate pointer for tx result

        public static Hash CalculatePointerForTxResult(Hash txId)
        {
            return txId.CalculateHashWith((Hash)"TransactionResult".CalculateHash());
        }

        #endregion
    }
}