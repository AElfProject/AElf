using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

namespace AElf.Cryptography.Certificate
{
    public class RSAKeyPair
    {
        public RSAKeyPair(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }
        //public static int AddressLength { get; } = 18;

        public AsymmetricKeyParameter PrivateKey { get; }
        public AsymmetricKeyParameter PublicKey { get; }

        //TODO: Add GetEncodedPublicKey case [Case]
        public byte[] GetEncodedPublicKey(bool compressed = false)
        {
            var keyParam = PublicKey;
            var info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParam);
            return info.GetEncoded();
        }

//        public byte[] GetAddress()
//        {
//            return GetEncodedPublicKey().Take(AddressLength).ToArray();
//        } 

//        public string GetAddressHex()
//        {
//            return "0x" + BitConverter.ToString(GetAddress()).Replace("-", string.Empty).ToLower();
//        }
    }
}