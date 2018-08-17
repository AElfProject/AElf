using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <inheritdoc />
    public class ResourcePath : IResourcePath
    {
        private Hash _chainId;
        private ulong _roundNumber;
        private Hash _blockProducerAddress;

        private Hash _dataProviderHash;
        private Hash _keyHash;

        public Hash StateHash
        {
            get
            {
                if (_chainId == null || _blockProducerAddress == null)
                {
                    throw new InvalidOperationException("Should set chain id and bp address before calculating state hash");
                }
                return new Hash(_chainId.CalculateHashWith(_blockProducerAddress)).CalculateHashWith(_roundNumber);
            }
        }

        public Hash ResourcePathHash => _dataProviderHash.CalculateHashWith(_keyHash);

        public Hash ResourcePointerHash => StateHash.CalculateHashWith(ResourcePathHash);

        /// <inheritdoc />
        public ResourcePath RemoveState()
        {
            _roundNumber = 0;
            _blockProducerAddress = null;
            return this;
        }

        /// <inheritdoc />
        public ResourcePath RemovePath()
        {
            _dataProviderHash = null;
            _keyHash = null;
            return this;
        }

        public ResourcePath SetChainId(Hash chainId)
        {
            _chainId = chainId;
            return this;
        }
        
        public ResourcePath SetRoundNumber(ulong roundNumber)
        {
            _roundNumber = roundNumber;
            return this;
        }
        
        public ResourcePath SetBlockProducerAddress(Hash blockProducerAddress)
        {
            _blockProducerAddress = blockProducerAddress;
            return this;
        }

        public ResourcePath SetDataProvider(Hash dataProviderHash)
        {
            _dataProviderHash = dataProviderHash;
            return this;
        }
        
        public ResourcePath SetDataKey(Hash keyHash)
        {
            _keyHash = keyHash;
            return this;
        }

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
        /// <param name="addrFuncSig">should be the key of the function metadata, which is [contract address].[function name]</param>
        /// <returns></returns>
        public static Hash CalculatePointerForMetadata(Hash chainId, string addrFuncSig)
        {
            return chainId.CalculateHashWith((Hash) ("Metadata" + addrFuncSig).CalculateHash());
        }

        /// <summary>
        /// Calculate pointer hash for using block height get block hash
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForGettingBlockHashByHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, "HeightHashMap".CalculateHash(),
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
            return HashExtensions.CalculateHashOfHashList(chainId, blockHash, "PathsCount".CalculateHash());
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