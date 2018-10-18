using System;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Node.CrossChain
{
    public class CrossChainHelper
    {
        private readonly Hash _chainId;
        private readonly IStateStore _stateStore;

        public CrossChainHelper(Hash chainId, IStateStore stateStore)
        {
            _chainId = chainId;
            _stateStore = stateStore;
        }

        /// <summary>
        /// Get merkle path stored by contract.
        /// </summary>
        /// <param name="contractAddressHash"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal MerklePath GetMerklePath(Address contractAddressHash, ulong height)
        {
            var bytes = GetBytes<MerklePath>(
                Hash.FromMessage(new UInt64Value {Value = height}), contractAddressHash,
                GlobalConfig.AElfTxRootMerklePathInParentChain);
            return MerklePath.Parser.ParseFrom(bytes);
        }
        
        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="contractAddressHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        private byte[] GetBytes<T>(Hash keyHash, Address contractAddressHash, string resourceStr = "") where T : IMessage, new()
        {
            //Console.WriteLine("resourceStr: {0}", dataPath.ResourcePathHash.ToHex());
            var dp = NewDataProvider.GetRootDataProvider(_chainId, contractAddressHash);
            dp.StateStore = _stateStore;
            return resourceStr != ""
                ? dp.GetDataProvider(resourceStr).GetAsync<T>(keyHash).Result
                : dp.GetAsync<T>(keyHash).Result;
        }

        internal ulong GetBoundParentChainHeight(Address contractAddressHash, ulong height)
        {
            var bytes = GetBytes<UInt64Value>(
                Hash.FromMessage(new UInt64Value {Value = height}), contractAddressHash,
                GlobalConfig.AElfBoundParentChainHeight);
            return UInt64Value.Parser.ParseFrom(bytes).Value;
        }
        
        internal ParentChainBlockInfo GetBoundParentChainBlockInfo(Address contractAddressHash, ulong height)
        {
            var bytes = GetBytes<ParentChainBlockInfo>(Hash.FromMessage(new UInt64Value {Value = height}), contractAddressHash,
                GlobalConfig.AElfParentChainBlockInfo);
            return ParentChainBlockInfo.Parser.ParseFrom(bytes);
        }
    }
}