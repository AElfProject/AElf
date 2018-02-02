using System;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractInvokerZero : ISmartContractInvoker
    {
        private SmartContractZero _contract;
        private readonly string _methodName;
        private readonly object[] _objs;

        private static Type _type = typeof(SmartContractZero);
        
        public SmartContractInvokerZero(
            SmartContractZero contract,string methodName,params object[] objs)
        {
            _contract = contract;
            _methodName = methodName;
            _objs = objs;
        }

        public async Task InvokeAsync(IAccountDataProvider accountDataProvider)
        {
            //First step, setup data access driver
        }
    }
}