using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainController.CrossChain
{
    public class CrossChainHelper
    {
        private readonly Hash _chainId;

        private Address SideChainContractAddress =>
            AddressHelpers.GetSystemContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()),
                SmartContractType.SideChainContract.ToString());

        private readonly IStateStore _stateStore;

        private DataProvider DataProvider
        {
            get
            {
                var dp = DataProvider.GetRootDataProvider(_chainId, SideChainContractAddress);
                dp.StateStore = _stateStore;
                return dp;
            }
        }

        public CrossChainHelper(Hash chainId, IStateStore stateStore)
        {
            _chainId = chainId;
            _stateStore = stateStore; 
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        internal byte[] GetBytes<T>(Hash keyHash, string resourceStr = "") where T : IMessage, new()
        {
            //Console.WriteLine("resourceStr: {0}", dataPath.ResourcePathHash.ToHex());
            return resourceStr != ""
                ? DataProvider.GetChild(resourceStr).GetAsync<T>(keyHash).Result
                : DataProvider.GetAsync<T>(keyHash).Result;
        }
    }
}