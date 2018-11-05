using AElf.Kernel;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Storages;
using AElf.SmartContract;

namespace AElf.Node.CrossChain
{
    public class CrossChainInfo : ICrossChainInfo
    {
        private readonly CrossChainHelper _crossChainHelper;

        public CrossChainInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
            _crossChainHelper = new CrossChainHelper(chainId, stateStore);
        }
        
        /// <summary>
        /// Get merkle path of transaction root in parent chain.
        /// </summary>
        /// <param name="contractAddress">Side chain contract address.</param>
        /// <param name="blockHeight">Child chain block height.</param>
        /// <returns></returns>
        public MerklePath GetTxRootMerklePathInParentChain(Address contractAddress, ulong blockHeight)
        {
            return _crossChainHelper.GetMerklePath(contractAddress, blockHeight);
        }

        public ulong GetBoundParentChainHeight(Address contractAddressHash, ulong height)
        {
            return _crossChainHelper.GetBoundParentChainHeight(contractAddressHash, height);
        }

        public ParentChainBlockInfo GetBoundParentChainBlockInfo(Address contractAddressHash, ulong height)
        {
            return _crossChainHelper.GetBoundParentChainBlockInfo(contractAddressHash, height);
        }
    }
}