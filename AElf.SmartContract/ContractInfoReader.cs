using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using Google.Protobuf;

namespace AElf.SmartContract
{
    public class ContractInfoReader
    {
        private readonly Hash _chainId;
        private readonly IStateStore _stateStore;

        public ContractInfoReader(Hash chainId, IStateStore stateStore)
        {
            _chainId = chainId;
            _stateStore = stateStore; 
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        public async Task<byte[]> GetBytesAsync<T>(Address contractAddress, Hash keyHash, string resourceStr = "") where T : IMessage, new()
        {
            //Console.WriteLine("resourceStr: {0}", dataPath.ResourcePathHash.ToHex());
            var dp = DataProvider.GetRootDataProvider(_chainId, contractAddress);
            dp.StateStore = _stateStore;

            if (resourceStr == "") return await dp.GetAsync<T>(keyHash);
            var resourceDataProvider = dp.GetChild(resourceStr);
            return await resourceDataProvider.GetAsync<T>(keyHash);
        }
    }
}