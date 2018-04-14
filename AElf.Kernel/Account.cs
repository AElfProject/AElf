using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        public Account(Hash address)
        {
            Address = address;
        }  

        public Account():this(Hash.Zero)
        {
            
        }

        public Hash Address { get; set; }

        public virtual byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public Hash GetAddress()
        {
            return Address;
        }
    }
}