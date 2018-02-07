using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        private readonly Hash<IAccount> _address;

        public Account(Hash<IAccount> address)
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
