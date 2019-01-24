using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
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
        private Address CrossChainContractAddress =>
            ContractHelpers.GetCrossChainContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
        
        public CrossChainInfoReader(IStateManager stateManager)
        {
            var chainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
            _contractInfoReader = new ContractInfoReader(chainId, stateManager);
        }

        /// <summary>
        /// Return merkle path of transaction root in parent chain.
        /// </summary>
        /// <param name="blockHeight">Child chain block height.</param>
        /// <returns></returns>
        public async Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<MerklePath>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = blockHeight}),
                            GlobalConfig.AElfTxRootMerklePathInParentChain);
            return bytes == null ? null : MerklePath.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Return height of parent chain block which indexed the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight"></param>
        /// <returns></returns>
        public async Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = localChainHeight}),
                            GlobalConfig.AElfBoundParentChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return current height of parent chain block stored locally.
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> GetParentChainCurrentHeightAsync()
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(CrossChainContractAddress,
                            Hash.FromString(GlobalConfig.AElfCurrentParentChainHeight));
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return current height of side chain block stored locally.
        /// </summary>
        /// <param name="chainId">Side chain id.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ulong> GetSideChainCurrentHeightAsync(int chainId)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<UInt64Value>(CrossChainContractAddress, Hash.FromRawBytes( chainId.DumpByteArray()),
                GlobalConfig.AElfCurrentSideChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Return side chain blocks indexed in given parent chain height. 
        /// </summary>
        /// <param name="height">Self(Parent) chain height.</param>
        /// <returns></returns>
        public async Task<IndexedSideChainBlockInfoResult> GetIndexedSideChainBlockInfoResult(ulong height)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<IndexedSideChainBlockInfoResult>(CrossChainContractAddress,
                Hash.FromMessage(new UInt64Value {Value = height}),
                GlobalConfig.IndexedSideChainBlockInfoResult);
            return bytes == null? null : IndexedSideChainBlockInfoResult.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Get info of parent chain block which indexes the local chain block at <see cref="localChainHeight"/>
        /// </summary>
        /// <param name="localChainHeight">Self(Side) chain height</param>
        /// <returns></returns>
        public async Task<ParentChainBlockInfo> GetBoundParentChainBlockInfoAsync(ulong localChainHeight)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<ParentChainBlockInfo>(CrossChainContractAddress,
                            Hash.FromMessage(new UInt64Value {Value = localChainHeight}),
                            GlobalConfig.AElfParentChainBlockInfo);
            return bytes == null ? null : ParentChainBlockInfo.Parser.ParseFrom(bytes);
        }
    }
}