using System;
using System.Linq;
using AElf.Cryptography.ECDSA;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <summary>
    /// Account.
    /// </summary>
    public class Account : IAccount
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public Account(Hash accountHash)
        {
            AccountHash = accountHash;
        }  

        // ReSharper disable once MemberCanBeProtected.Global
        public Account():this(Hash.Zero)
        {
            
        }

        public virtual byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Hash AccountHash { get; }

        public Hash GetAccountHash()
        {
            return AccountHash;
        }

        public byte[] GetAddress()
        {
            return AccountHash.Value.Take(ECKeyPair.AddressLength).ToArray();
        }
        
        public string GetAddressHex()
        {
            return BitConverter.ToString(GetAddress()).Replace("-", string.Empty).ToLower();
        }
    }
}