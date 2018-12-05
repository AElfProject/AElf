using Org.BouncyCastle.Math;

namespace AElf.Cryptography.ECDSA
{
    public class ECSignature
    {
        public byte[] SigBytes { get; }

        public ECSignature(byte[] sigBytes)
        {
            SigBytes = sigBytes;
        }
    }
}