using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero: ISmartContract
    {
        private const string SmartContractMapKey = "SmartContractMap";
        
        private IAccountDataProvider _accountDataProvider;

        public async Task InitializeAsync(IAccountDataProvider accountDataProvider)
        {
            _accountDataProvider = accountDataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IAccount caller, string methodname, params object[] objs)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);
            
            await (Task) member.Invoke(this, objs);
        }
        
        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SmartContractMapKey);
            await smartContractMap.SetAsync(reg.Hash, reg);
        }
    }
}