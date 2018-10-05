using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

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

                return Hash.FromTwoHashes(
                    Hash.FromBytes(ChainId.CalculateHashWith(BlockProducerAddress)),
                    Hash.FromMessage(new UInt64Value(){Value = BlockHeight})
                ).OfType(HashType.StateHash);
            }
            set => _stateHash = value;
        }

        public Hash ResourcePathHash => new List<Hash> {
            Hash.FromMessage(ContractAddress), DataProviderHash, KeyHash
        }.Aggregate(Hash.FromTwoHashes).OfType(HashType.ResourcePath);

        public Hash ResourcePointerHash => Hash.FromTwoHashes(
            StateHash,ResourcePathHash
        ).OfType(HashType.ResourcePointer);

        /// <summary>
        /// For pipeline setting.
        /// </summary>
        public Hash Key => ResourcePointerHash;
//            Hash.FromMessage( 
//            new Key
//        {
//            Type =  Type,
//            Value = ByteString.CopyFrom(ResourcePointerHash.GetHashBytes()),
//            HashType = (uint) HashType.ResourcePointer
//        });
            
        public string Type { get; set; }

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
        
        public DataPath SetBlockProducerAddress(Address blockProducerAddress)
        {
            BlockProducerAddress = blockProducerAddress;
            return this;
        }

        public DataPath SetAccountAddress(Address contractAddress)
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
            return Hash.FromBytes(chainId.CalculateHashWith(Hash.FromString("Metadata" + addrFuncSig)));
        }

        /// <summary>
        /// Calculate pointer hash for using block height get block hash
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForGettingBlockHashByHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, Hash.FromString("HeightHashMap"),
                Hash.FromMessage(new UInt64Value {Value = height}));
        }
        
        /// <summary>
        /// Calculate pointer hash of <see cref="BinaryMerkleTree"/> for transactions using chainId and chain height.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForTransactionsMerkleTreeByHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, Hash.FromString("TransactionsMerkleTree"),
                Hash.FromMessage(new UInt64Value {Value = height}));
        }
        
        /// <summary>
        /// Calculate pointer hash of <see cref="BinaryMerkleTree"/>
        /// for side chain transaction roots using chainId and chain height.
        /// </summary>
        /// /// <param name="chainId">Parent chainId</param>
        /// <param name="height">Height of parent chain.</param>
        /// <returns></returns>
        public static Hash CalculatePointerForSideChainTxRootsMerkleTreeByHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, Hash.FromString("SideChainTxRootsMerkleTree"),
                Hash.FromMessage(new UInt64Value {Value = height}));
        }
        
        /// <summary>
        /// Calculate pointer hash of <see cref="MerklePath"/>
        /// for tx root of a block indexed by parent chain.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Hash CalculatePointerForIndexedTxRootMerklePathByHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, Hash.FromString("IndexedBlockTxRoot"),
                Hash.FromMessage(new UInt64Value {Value = height}));
        }

        public static Hash CalculatePointerForParentChainHeightByChildChainHeight(Hash chainId, ulong height)
        {
            return HashExtensions.CalculateHashOfHashList(chainId, Hash.FromString("ParentChainHeight"),
                Hash.FromMessage(new UInt64Value {Value = height}));
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
        public static Address CalculateAccountAddress(Address parentAccount, ulong nonce)
        {
            return Address.FromBytes(HashExtensions.CalculateHashOfHashList(
                Hash.FromMessage(parentAccount),
                Hash.FromMessage(new UInt64Value
            {
                Value = nonce
            })).ToByteArray());
        }
        
        #endregion

        #region Calculate pointer for tx result

        public static Hash CalculatePointerForTxResult(Hash txId)
        {
            return HashExtensions.CalculateHashOfHashList(txId, Hash.FromString("TransactionResult"));
        }

        #endregion

        
    }

    public class DataPath<T> where T : IMessage, new()
    {
        private readonly DataPath _dataPath;
        public DataPath(DataPath dataPath)
        {
            _dataPath = dataPath;
        }
    }
}