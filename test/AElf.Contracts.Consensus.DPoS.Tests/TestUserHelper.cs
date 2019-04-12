using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.Consensus.DPoS
{
    public struct User
    {
        public ECKeyPair KeyPair { get; set; }
        public Address Address { get; set; }
        public string PublicKey { get; set; }

        public static implicit operator ECKeyPair(User user)
        {
            return user.KeyPair;
        }

        public static implicit operator Address(User user)
        {
            return user.Address;
        }

        public static implicit operator string(User user)
        {
            return user.PublicKey;
        }
    }
    
    public class TestUserHelper
    {
        public static User GenerateNewUser()
        {
            var callKeyPair = CryptoHelpers.GenerateKeyPair();
            var callAddress = Address.FromPublicKey(callKeyPair.PublicKey);
            var callPublicKey = callKeyPair.PublicKey.ToHex();

            return new User
            {
                KeyPair = callKeyPair,
                Address = callAddress,
                PublicKey = callPublicKey
            };
        }
    }
}