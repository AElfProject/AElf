using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class SideChainManager : ISideChainManager
    {
        private IDataStore _dataStore;
        private readonly Hash _key = "SideChainIdList".CalculateHash();
        public SideChainManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }


        public async Task AddSideChain(Hash chainId)
        {
            var idList = await GetSideChainIdList();
            idList = idList ?? new SideChainIdList();
            idList.ChainIds.Add(chainId);
            await _dataStore.InsertAsync(_key, idList);
        }

        public async Task<SideChainIdList> GetSideChainIdList()
        {
            return await _dataStore.GetAsync<SideChainIdList>(_key);
        }
    }
}