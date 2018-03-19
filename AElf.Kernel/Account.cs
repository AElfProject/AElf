using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        private readonly IHash<IAccount> _address;

        public Account(IHash<IAccount> address)
        {
            _address = address;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public IHash<IAccount> GetAddress()
        {
            return _address;
        }
    }
}
