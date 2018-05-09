using System.Runtime.InteropServices.ComTypes;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace AElf.Kernel.Crypto.ECDSA
{
    public class ECKeyPair
    {
        public ECPrivateKeyParameters PrivateKey { get; private set; }
        public ECPublicKeyParameters PublicKey { get; private set; }

        public ECKeyPair(ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] GetPublicKey(bool compressed = false)
        {
            return PublicKey.Q.GetEncoded(compressed);
        }

        public static ECKeyPair FromPublicKey(byte[] publicKey)
        {
            ECPublicKeyParameters pubKey 
                = new ECPublicKeyParameters(Parameters.Curve.Curve.DecodePoint(publicKey), Parameters.DomainParams);
            
            ECKeyPair k = new ECKeyPair(null, pubKey);

            return k;
        }
    }
}