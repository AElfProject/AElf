using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.TestKit
{
    public interface IAccount
    {
        byte[] Sign(byte[] data);
        byte[] GetPublicKey();
    }

    public class Account : IAccount
    {
        private ECKeyPair _keyPair = CryptoHelper.GenerateKeyPair();

        public byte[] Sign(byte[] data)
        {
            return CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, data);
        }

        public byte[] GetPublicKey()
        {
            return _keyPair.PublicKey;
        }
    }
}