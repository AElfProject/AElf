using System;

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
        }
    }
}