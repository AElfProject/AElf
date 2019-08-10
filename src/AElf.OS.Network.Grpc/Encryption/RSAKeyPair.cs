using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

namespace AElf.OS.Network.Grpc.Helpers
{
    public class RSAKeyPair
    {
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
    }
}