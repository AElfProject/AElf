using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;
        
        public async void InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
        }

        public async Task InvokeAsync(IHash<IAccount> caller, string methodname, params object[] objs)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);
            
            await (Task) member.Invoke(this, objs);
        }
        
        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = (IAccountDataProvider) await _accountDataProvider.GetMapAsync(SMART_CONTRACT_MAP_KEY);
            await smartContractMap.SetAsync(reg.Hash, reg);
        }
    }
}