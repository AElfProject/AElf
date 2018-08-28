using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace AElf.Kernel
{
    public partial class DataPath
    {
        private Hash _stateHash;
        
        public Hash StateHash
        {
            get
            {
                if (_stateHash != null)
                {
                    return _stateHash.OfType(HashType.StateHash);
                }
                
                if (ChainId == null || BlockProducerAddress == null)
                {
                    throw new InvalidOperationException("Should set chain id and bp address before calculating state hash");
                }

                return ((Hash) new Hash(ChainId.CalculateHashWith(BlockProducerAddress)).CalculateHashWith(
                    BlockHeight)).OfType(HashType.StateHash);
            }
            set => _stateHash = value;
        }

        public Hash ResourcePathHash => HashExtensions
            .CalculateHashOfHashList(ContractAddress, DataProviderHash, KeyHash).OfType(HashType.ResourcePath);

        public Hash ResourcePointerHash =>
            ((Hash) StateHash.CalculateHashWith(ResourcePathHash)).OfType(HashType.ResourcePointer);

        /// <summary>
        /// For pipeline setting.
        /// </summary>
        public Hash Key => new Key
        {
            Type = (uint) Type,
            Value = ByteString.CopyFrom(ResourcePointerHash.GetHashBytes()),
            HashType = (uint) HashType.ResourcePointer
        }.ToByteArray();
            
        public Types Type { get; set; }

        public DataPath SetChainId(Hash chainId)
        {
            ChainId = chainId;
            return this;
        }
        
        public DataPath SetBlockHeight(ulong blockHeight)
        {
            BlockHeight = blockHeight;
            return this;
        }
        
        public DataPath SetBlockProducerAddress(Hash blockProducerAddress)
        {
            BlockProducerAddress = blockProducerAddress;
            return this;
        }

        public DataPath SetAccountAddress(Hash contractAddress)
        {
            ContractAddress = contractAddress;
            return this;
        }

        public DataPath SetDataProvider(Hash dataProviderHash)
        {
            DataProviderHash = dataProviderHash;
            return this;
        }
        
        public DataPath SetDataKey(Hash keyHash)
        {
            KeyHash = keyHash;
            return this;
        }

        public bool AreYouOk()
        {
            return ChainId != null
                   && BlockProducerAddress != null
                   && ContractAddress != null
                   && DataProviderHash != null
                   && KeyHash != null;
        }

        /*
         * Directly calculate pointer value zone
         */
        
        #region Calculate pointer for chain context
        
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

        #region Calculate pointer for tx result

        public static Hash CalculatePointerForTxResult(Hash txId)
        {
            return txId.CalculateHashWith((Hash)"TransactionResult".CalculateHash());
        }

        #endregion
        
        public enum Types
        {
            UInt64Value = 0,
            Hash,
            BlockBody,
            BlockHeader,
            Chain,
            Change,
            SmartContractRegistration,
            TransactionResult,
            Transaction,
            FunctionMetadata,
            SerializedCallGraph,
            SideChain,
            WorldState,
            Miners,
            BlockProducer,
            Round,
            AElfDPoSInformation,
            Int32Value,
            StringValue,
            Timestamp,
            SInt32Value
        }
    }

}