using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        public Account(IHash<IAccount> address)
        {
            Address = address;
        }  

        public Account()
        {
            
        }

        public IHash<IAccount> Address { get; set; }

        public virtual byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public IHash<IAccount> GetAddress()
        {
            return this.Address;
        }
    }
}
