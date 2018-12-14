using System;
using Secp256k1Net;
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
            ECPrivateKeyParameters newPrivateKeyParam = (ECPrivateKeyParameters) cipherKeyPair.Private;
            ECPublicKeyParameters newPublicKeyParam = (ECPublicKeyParameters) cipherKeyPair.Public;

            PrivateKey = newPrivateKeyParam.D.ToByteArrayUnsigned();
            PublicKey = newPublicKeyParam.Q.GetEncoded(false);
        }

//        public byte[] GetEncodedPublicKey(bool compressed = false)
//        {
//            return PublicKey.Q.GetEncoded(compressed);
//        }

        // todo remove if not needed
//        public static ECKeyPair FromPublicKey(byte[] publicKey)
//        {
//            ECPublicKeyParameters pubKey
//                = new ECPublicKeyParameters(ECParameters.Curve.Curve.DecodePoint(publicKey), ECParameters.DomainParams);
//
//            ECKeyPair k = new ECKeyPair(null, pubKey);
//
//            return k;
//        }
    }
}