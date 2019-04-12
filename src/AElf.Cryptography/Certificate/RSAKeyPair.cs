using System;
using System.Linq;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using ECParameters = System.Security.Cryptography.ECParameters;

namespace AElf.Cryptography.Certificate
{
    public class RSAKeyPair
    {
        //public static int AddressLength { get; } = 18;

        public AsymmetricKeyParameter PrivateKey { get; private set; }
        public AsymmetricKeyParameter PublicKey { get; private set; }

        public RSAKeyPair(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] GetEncodedPublicKey(bool compressed = false)
        {
            AsymmetricKeyParameter keyParam = PublicKey;
            var info = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo (keyParam);
            return info.GetEncoded ();
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