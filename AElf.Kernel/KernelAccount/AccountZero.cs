﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class AccountZero : IAccount
    {
        private readonly SmartContractZero _smartContractZero;

        public AccountZero(SmartContractZero smartContractZero)
        {
            _smartContractZero = smartContractZero;
        }
       
        public Hash GetAddress()
        {
            return Hash.Zero;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}