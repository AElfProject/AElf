using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero: ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;
        
        
        public Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            this._accountDataProvider = dataProvider;
        }

        public async Task InvokeAsync(IHash<IAccount> caller, 
            string methodname, params object[] objs)
        {

            var type = typeof(SmartContractZero);

            var member = type.GetMethod(methodname);


            await (Task) member.Invoke(this, objs);

        }

        
        
        
        public async Task RegisterSmartContrace(SmartContractRegistration reg)
        {
            
            // My Sql
            
            
            var smartContractMap = (IAccountDataProvider)
                await _accountDataProvider.GetMapAsync("SmartContractMap");
            await smartContractMap.SetAsync(reg.Hash, reg);

        }
    }
}