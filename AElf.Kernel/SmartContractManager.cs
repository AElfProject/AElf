using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class SmartContractManager: ISmartContractManager
    {
        private readonly WorldState _worldState;
        private readonly IAccountManager _accountManager;

        public SmartContractManager(WorldState worldState, IAccountManager accountManager)
        {
            _worldState = worldState;
            _accountManager = accountManager;
        }

        public async Task<ISmartContract> GetAsync(IAccount account)
        {
            // if new account, accountDataProvider will be automatically created and stored in worldstate 
            var accoountDataProvider = _worldState.GetAccountDataProviderByAccount(account);
            var smartContract = new SmartContract(_accountManager, _worldState);
            await smartContract.InititalizeAsync(accoountDataProvider);
            return smartContract;
        }
    }
}