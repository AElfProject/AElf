using System;

namespace AElf.Kernel
{
    public class Account : IAccount
    {
        public int Amount { get; set; }

        public IHash<IAccount> GetAddress()
        {
            return new Hash<IAccount>(this.GetSHA256Hash());
        }

        public void Invoke(string methodName, params string[] values)
        {
            throw new NotImplementedException();
        }
    }
}
