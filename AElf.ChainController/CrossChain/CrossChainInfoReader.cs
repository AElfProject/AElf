using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainController.CrossChain
{
    public class CrossChainInfoReader : ICrossChainInfoReader
    {
        private readonly ContractInfoReader _contractInfoReader;

        public CrossChainInfoReader(IStateManager stateManager)
        {
            _contractInfoReader = new ContractInfoReader(stateManager);
        }

        /// <summary>
        /// Return merkle path of transaction root in parent chain.
        /// </summary>
        /// <param name="blockHeight">Child chain block height.</param>
        /// <returns></returns>
        public async Task<MerklePath> GetTxRootMerklePathInParentChainAsync(int chainId, ulong blockHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<MerklePath>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId),
                Hash.FromMessage(new UInt64Value {Value = blockHeight}),
                GlobalConfig.AElfTxRootMerklePathInParentChain);
            return bytes == null ? null : MerklePath.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Return height of parent chain block which indexed the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight"></param>
        /// <returns></returns>
        public async Task<ulong> GetBoundParentChainHeightAsync(int chainId, ulong localChainHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId),
                Hash.FromMessage(new UInt64Value {Value = localChainHeight}), GlobalConfig.AElfBoundParentChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return current height of parent chain block stored locally.
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> GetParentChainCurrentHeightAsync(int chainId)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId),
                Hash.FromString(GlobalConfig.AElfCurrentParentChainHeight));
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return current height of side chain block stored locally.
        /// </summary>
        /// <param name="chainId">Side chain id.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId), Hash.FromRawBytes(sideChainId.DumpByteArray()),
                GlobalConfig.AElfCurrentSideChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return side chain blocks indexed in given parent chain height. 
        /// </summary>
        /// <param name="height">Self(Parent) chain height.</param>
        /// <returns></returns>
        public async Task<IndexedSideChainBlockInfoResult> GetIndexedSideChainBlockInfoResult(int chainId, ulong height)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<IndexedSideChainBlockInfoResult>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId),
                Hash.FromMessage(new UInt64Value {Value = height}), GlobalConfig.IndexedSideChainBlockInfoResult);
            return bytes == null ? null : IndexedSideChainBlockInfoResult.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Get info of parent chain block which indexes the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight">Self(Side) chain height</param>
        /// <returns></returns>
        public async Task<ParentChainBlockInfo> GetBoundParentChainBlockInfoAsync(int chainId, ulong localChainHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<ParentChainBlockInfo>(chainId,
                ContractHelpers.GetCrossChainContractAddress(chainId),
                Hash.FromMessage(new UInt64Value {Value = localChainHeight}),
                GlobalConfig.AElfParentChainBlockInfo);
            return bytes == null ? null : ParentChainBlockInfo.Parser.ParseFrom(bytes);
        }
    }
}