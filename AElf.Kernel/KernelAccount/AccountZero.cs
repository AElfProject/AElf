using System;

namespace AElf.Kernel.KernelAccount
{
    public class AccountZero:IAccount
    {
        private SmartContractZero _smartContractZero;


        
        public AccountZero(SmartContractZero smartContractZero)
        {
            _smartContractZero = smartContractZero;

        }


        public IHash<IAccount> GetAddress()
        {
            return Hash<IAccount>.Zero;
        }

        public ISmartContractInvoker CreateInvoker(string methodName, params object[] values)
        {
            throw new NotImplementedException();
        }

    }
}