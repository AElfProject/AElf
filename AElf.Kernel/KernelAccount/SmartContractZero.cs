using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero: ISmartContract
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;
        
        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
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
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            await smartContractMap.SetAsync(reg.Hash, reg);
        }
    }
}