using System;
using AElf.Common;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair
    {
        public byte[] PrivateKey { get; }
        public byte[] PublicKey { get; }

        internal ECKeyPair(byte[] privateKey, byte[] publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey.LeftPad(Secp256k1.PRIVKEY_LENGTH);
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

            PrivateKey = newPrivateKeyParam.D.ToByteArrayUnsigned().LeftPad(Secp256k1.PRIVKEY_LENGTH);
            PublicKey = newPublicKeyParam.Q.GetEncoded(false);
        }
    }
}