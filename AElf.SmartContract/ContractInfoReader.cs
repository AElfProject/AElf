using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using Google.Protobuf;

namespace AElf.SmartContract
{
    public class ContractInfoReader
    {
        private readonly IStateManager _stateManager;

        public ContractInfoReader(IStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        public async Task<byte[]> GetBytesAsync<T>(int chainId, Address contractAddress, Hash keyHash,
            string resourceStr = "")
            where T : IMessage, new()
        {
            //Console.WriteLine("resourceStr: {0}", dataPath.ResourcePathHash.ToHex());
            var dp = DataProvider.GetRootDataProvider(chainId, contractAddress);
            dp.StateManager = _stateManager;

            if (resourceStr == "") return await dp.GetAsync<T>(keyHash);
            var resourceDataProvider = dp.GetChild(resourceStr);
            return await resourceDataProvider.GetAsync<T>(keyHash);
        }
    }
}