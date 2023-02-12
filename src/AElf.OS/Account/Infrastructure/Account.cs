using AElf.Cryptography.ECDSA;

namespace AElf.OS.Account.Infrastructure;

public class Account
{
    public Account(string address)
    {
        AccountName = address;
    }

    public ECKeyPair KeyPair { get; set; }
    public string AccountName { get; }
}