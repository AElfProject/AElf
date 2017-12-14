<<<<<<< HEAD
﻿using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        private Address m_address;

        Account(Address address) {
            this.m_address = address;
        }

        IHash<IAccount> IAccount.GetAddress()
        {
            return this.m_address;
=======
﻿namespace AElf.Kernel
{
    public class Account : IAccount
    {
        public IHash<IAccount> GetAddress()
        {
            throw new System.NotImplementedException();
>>>>>>> a07f680fb13afbf511802b20cb65014b9ac9481d
        }
    }
}