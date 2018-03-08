using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Tests.Contracts
{
    public class SmartContract : ISmartContract
    {
        
        /*
         * all contracts written by user must inherit this class for invoke later
         */
        
        private IAccountDataProvider _accountDataProvider;
        
        public Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            return Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash<IAccount> caller, string methodname, params object[] objs)
        {
            var type = GetType();
            var member = type.GetMethod(methodname);
        
            await (Task) member.Invoke(this, objs);
        }

    }
}