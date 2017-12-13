using System;

namespace AElf.Kernel
{
    public struct Address :IHash<IAccount>
    {
        bool IEquatable<IHash>.Equals(IHash other)
        {
            throw new NotImplementedException();
        }

        byte[] IHash.GetHashBytes()
        {
            throw new NotImplementedException();
        }
    }


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