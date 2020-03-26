using System.Threading;
using AElf.Cryptography.ECDSA;

namespace AElf.OS.Account.Infrastructure
{
    public class Account
    {
        public ECKeyPair KeyPair { get; set; }
        public string AccountName { get; }

        public Account(string address)
        {
            AccountName = address;
        }
    }
}