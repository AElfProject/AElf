using System;
using Secp256k1Net;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair
    {
        public byte[] PrivateKey { get; private set; }
        private readonly byte[] _secp256K1PubKey;
        private byte[] _uncompressedPubKey;

        public byte[] PublicKey
        {
            get
            {
                if (_uncompressedPubKey == null)
                {
                    _uncompressedPubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];

                    using (var secp256k1 = new Secp256k1())
                    {
                        secp256k1.PublicKeySerialize(_uncompressedPubKey, _secp256K1PubKey);
                    }
                }

                return _uncompressedPubKey;
            }
        }

        internal ECKeyPair(byte[] privateKey, byte[] secp256k1PubKey)
        {
            _secp256K1PubKey = secp256k1PubKey;
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

            byte[] readPrivateKey = newPrivateKeyParam.D.ToByteArrayUnsigned();
            byte[] readPublicKey = newPublicKeyParam.Q.GetEncoded(false);

            _secp256K1PubKey = new byte[Secp256k1.PUBKEY_LENGTH];
            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.PublicKeyParse(_secp256K1PubKey, readPublicKey);
            }

            PrivateKey = readPrivateKey;
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