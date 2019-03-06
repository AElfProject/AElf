using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair
    {
        public byte[] PrivateKey { get; }
        public byte[] PublicKey { get; }

        internal ECKeyPair(byte[] privateKey, byte[] publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public ECKeyPair(AsymmetricCipherKeyPair cipherKeyPair)
        {
            if (cipherKeyPair == null)
            {
                throw new Exception($"Invalid input null for {nameof(cipherKeyPair)}");
            }

            // Extract bouncy params
            var newPrivateKeyParam = (ECPrivateKeyParameters) cipherKeyPair.Private;
            var newPublicKeyParam = (ECPublicKeyParameters) cipherKeyPair.Public;

            PrivateKey = newPrivateKeyParam.D.ToByteArrayUnsigned();
            PublicKey = newPublicKeyParam.Q.GetEncoded(false);
        }
    }
}