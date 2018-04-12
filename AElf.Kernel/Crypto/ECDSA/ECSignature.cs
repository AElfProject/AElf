using Org.BouncyCastle.Math;

namespace AElf.Kernel.Crypto.ECDSA
{
    public class ECSignature
    {
        public BigInteger[] Signature { get; private set; }

        public ECSignature(BigInteger[] signature)
        {
            Signature = signature;
        }

        public byte[] R
        {
            get
            {
                if (Signature == null || Signature.Length != 2)
                    return null;

                return Signature[0].ToByteArrayUnsigned();
            }
        }
        
        public byte[] S
        {
            get
            {
                if (Signature == null || Signature.Length != 2)
                    return null;

                return Signature[1].ToByteArrayUnsigned();
            }
        }
    }
}