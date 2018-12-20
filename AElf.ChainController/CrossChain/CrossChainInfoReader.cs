using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainController.CrossChain
{
    public class CrossChainInfoReader : ICrossChainInfoReader
    {
        private readonly ContractInfoReader _contractInfoReader;
        private Address CrossChainContractAddress =>
            ContractHelpers.GetCrossChainContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId));
        public CrossChainInfoReader(IStateStore stateStore)
        {
            var chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
            _contractInfoReader = new ContractInfoReader(chainId, stateStore);
        }

        /// <summary>
        /// Get merkle path of transaction root in parent chain.
        /// </summary>
        /// <param name="blockHeight">Child chain block height.</param>
        /// <returns></returns>
        public MerklePath GetTxRootMerklePathInParentChain(ulong blockHeight)
        {
            var bytes = _contractInfoReader.GetBytes<MerklePath>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = blockHeight}),
                            GlobalConfig.AElfTxRootMerklePathInParentChain);
            return bytes == null ? null : MerklePath.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Get height of parent chain block which indexed the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight"></param>
        /// <returns></returns>
        public ulong GetBoundParentChainHeight(ulong localChainHeight)
        {
            var bytes = _contractInfoReader.GetBytes<UInt64Value>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = localChainHeight}),
                            GlobalConfig.AElfBoundParentChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Get current height of parent chain block stored locally
        /// </summary>
        /// <returns></returns>
        public ulong GetParentChainCurrentHeight()
        {
            var bytes = _contractInfoReader.GetBytes<UInt64Value>(CrossChainContractAddress,
                            Hash.FromString(GlobalConfig.AElfCurrentParentChainHeight));
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Get current height of side chain block stored locally
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ulong GetSideChainCurrentHeight(Hash chainId)
        {
            var bytes = _contractInfoReader.GetBytes<UInt64Value>(CrossChainContractAddress, Hash.FromMessage(chainId),
                GlobalConfig.AElfCurrentSideChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Get binary merkle tree of side chain transaction root by self chain height.
        /// </summary>
        /// <param name="height">Self chain height.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public BinaryMerkleTree GetMerkleTreeForSideChainTransactionRoot(ulong height)
        {
            var bytes = _contractInfoReader.GetBytes<MerklePath>(CrossChainContractAddress,
                Hash.FromMessage(new UInt64Value {Value = height}),
                GlobalConfig.AElfBinaryMerkleTreeForSideChainTxnRoot);
            return bytes == null ? null : BinaryMerkleTree.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Get info of parent chain block which indexes the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight">Local chain height</param>
        /// <returns></returns>
        public ParentChainBlockInfo GetBoundParentChainBlockInfo(ulong localChainHeight)
        {
            var bytes = _contractInfoReader.GetBytes<ParentChainBlockInfo>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = localChainHeight}),
                            GlobalConfig.AElfParentChainBlockInfo);
            return bytes == null ? null : ParentChainBlockInfo.Parser.ParseFrom(bytes);
        }
    }
}