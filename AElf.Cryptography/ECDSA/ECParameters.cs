using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    public static class ECParameters
    {
        public static readonly X9ECParameters Curve = SecNamedCurves.GetByName("secp256k1");
        public static readonly ECDomainParameters DomainParams = new ECDomainParameters (Curve.Curve, Curve.G, Curve.N, Curve.H);
    }
}