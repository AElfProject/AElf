using System;

namespace AElf.Kernel
{
    public class Account : IAccount
    {
        public int Amount { get; set; }

        public IHash<IAccount> GetAddress()
        {
            return new Hash<IAccount>(ExtensionMethods.GetHash(this));
        }

        public void Invoke(string methodName, params string[] values)
        {
            throw new NotImplementedException();
        }
    }
}
