using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainController.CrossChain
{
    public class CrossChainInfo : ICrossChainInfo
    {
        private readonly CrossChainHelper _crossChainHelper;
        private ILightChain LightChain { get;}

        private Address SideChainContractAddress =>
            AddressHelpers.GetSystemContractAddress(Hash.LoadHex(NodeConfig.Instance.ChainId),
                SmartContractType.SideChainContract.ToString());

        public CrossChainInfo(IStateStore stateStore, IChainService chainService)
        {
            var chainId = Hash.LoadHex(NodeConfig.Instance.ChainId);
            _crossChainHelper = new CrossChainHelper(chainId, stateStore);
            LightChain = chainService.GetLightChain(chainId);
        }

        /// <summary>
        /// Get merkle path of transaction root in parent chain.
        /// </summary>
        /// <param name="blockHeight">Child chain block height.</param>
        /// <returns></returns>
        public MerklePath GetTxRootMerklePathInParentChain(ulong blockHeight)
        {
            var bytes = _crossChainHelper.GetBytes<UInt64Value>(
                Hash.FromMessage(new UInt64Value {Value = blockHeight}), SideChainContractAddress,
                GlobalConfig.AElfTxRootMerklePathInParentChain);
            return MerklePath.Parser.ParseFrom(bytes);
        }

        /// <summary>
        /// Get height of parent chain block which indexed the local chain block at <see cref="height"/>
        /// </summary>
        /// <param name="height">Local chain height</param>
        /// <returns></returns>
        public ulong GetBoundParentChainHeight(ulong height)
        {
            var bytes = _crossChainHelper.GetBytes<UInt64Value>(
                Hash.FromMessage(new UInt64Value {Value = height}), SideChainContractAddress,
                GlobalConfig.AElfBoundParentChainHeight);
            return UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Get current height of parent chain block stored locally
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ulong> GetParentChainCurrentHeight()
        {
            var currentHeight = await LightChain.GetCurrentBlockHeightAsync();
            var bytes = _crossChainHelper.GetBytes<UInt64Value>(
                Hash.FromMessage(new UInt64Value {Value = currentHeight}), SideChainContractAddress,
                GlobalConfig.AElfCurrentParentChainHeight);
            return bytes == null ? 0 : UInt64Value.Parser.ParseFrom(bytes).Value;
        }

        /// <summary>
        /// Get info of parent chain block which indexed the local chain block at <see cref="height"/>
        /// </summary>
        /// <param name="height">Local chain height</param>
        /// <returns></returns>
        public ParentChainBlockInfo GetBoundParentChainBlockInfo(ulong height)
        {
            var bytes = _crossChainHelper.GetBytes<ParentChainBlockInfo>(
                Hash.FromMessage(new UInt64Value {Value = height}), SideChainContractAddress,
                GlobalConfig.AElfParentChainBlockInfo);
            return ParentChainBlockInfo.Parser.ParseFrom(bytes);
        }
        
    }
}