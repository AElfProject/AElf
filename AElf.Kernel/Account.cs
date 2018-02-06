using System;

namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        private readonly byte[] _address;

        public Account(byte[] address)
        {
            _address = address;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        IHash<IAccount> IAccount.GetAddress()
        {
            return new Hash<IAccount>(_address);
        }
    }
}
