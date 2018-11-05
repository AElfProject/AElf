using AElf.Common;
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
        private readonly IStateStore _stateStore;

        public CrossChainHelper(Hash chainId, IStateStore stateStore)
        {
            _chainId = chainId;
            _stateStore = stateStore;
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="contractAddressHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        internal byte[] GetBytes<T>(Hash keyHash, Address contractAddressHash, string resourceStr = "") where T : IMessage, new()
        {
            //Console.WriteLine("resourceStr: {0}", dataPath.ResourcePathHash.ToHex());
            var dp = DataProvider.GetRootDataProvider(_chainId, contractAddressHash);
            dp.StateStore = _stateStore;
            return resourceStr != ""
                ? dp.GetDataProvider(resourceStr).GetAsync<T>(keyHash).Result
                : dp.GetAsync<T>(keyHash).Result;
        }
    }
}