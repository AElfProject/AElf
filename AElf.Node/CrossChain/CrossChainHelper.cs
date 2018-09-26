using System;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Node.CrossChain
{
    public class CrossChainHelper
    {
        private readonly IStateDictator _stateDictator;

        public CrossChainHelper(IStateDictator stateDictator)
        {
            _stateDictator = stateDictator;
        }

        /// <summary>
        /// Get merkle path stored by contract.
        /// </summary>
        /// <param name="contractAddressHash"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal MerklePath GetMerklePath(Hash contractAddressHash, ulong height)
        {
            var bytes = GetBytes<MerklePath>(new UInt64Value {Value = height}.CalculateHash(), contractAddressHash,
                Globals.AElfTxRootMerklePathInParentChain);
            return MerklePath.Parser.ParseFrom(bytes);
        }
        
        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="contractAddressHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        private byte[] GetBytes<T>(Hash keyHash, Hash contractAddressHash, string resourceStr = "") where T : IMessage, new()
        {
            return resourceStr != ""
                ? _stateDictator.GetAccountDataProvider(contractAddressHash).GetDataProvider()
                    .GetDataProvider(resourceStr).GetAsync<T>(keyHash).Result
                : _stateDictator.GetAccountDataProvider(contractAddressHash).GetDataProvider()
                    .GetAsync<T>(keyHash).Result;
        }
    }
}