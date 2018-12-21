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

                return new List<Hash>()
                {
                    ChainId, 
                    Hash.FromMessage(BlockProducerAddress),
                    Hash.FromMessage(new UInt64Value(){Value = BlockHeight})
                }.Aggregate(Hash.FromTwoHashes).OfType(HashType.StateHash);
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
            
        public string Type { get; set; }

        public DataPath SetChainId(Hash chainId)
        {
            ChainId = chainId;
            return this;
        }
    }
}