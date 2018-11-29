using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair
    {
        public byte[] PrivateKey { get; private set; }
        public byte[] PublicKey { get; private set; }

        public ECKeyPair(byte[] privateKey, byte[] publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] SerializedPublicKey()
        {
            byte[] serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            
            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.PublicKeySerialize(serializedKey, PublicKey);
            }

            return serializedKey;
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