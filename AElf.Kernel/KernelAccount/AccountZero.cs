using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class AccountZero : IAccount
    {
        public SmartContractZero SmartContractZero { get; }

        public AccountZero(SmartContractZero smartContractZero)
        {
            SmartContractZero = smartContractZero;
        }
       
        public IHash<IAccount> GetAddress()
        {
            return Hash<IAccount>.Zero;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}