using Org.BouncyCastle.Math;

namespace AElf.Kernel.Crypto.ECDSA
{
    public class ECSignature
    {
        public BigInteger[] Signature { get; }

        public ECSignature(BigInteger[] signature)
        {
            Signature = signature;
        }

        public byte[] Encoded()
        {
            byte[] x = Signature[0].ToByteArrayUnsigned();
            byte[] y = Signature[1].ToByteArrayUnsigned();
            
            byte[] enc = new byte[x.Length + y.Length];
            x.CopyTo(enc, 0);
            y.CopyTo(enc, x.Length);
            
            return enc;
        }

        public byte[] R
        {
            get
            {
                if (Signature == null || Signature.Length != 2)
                    return null;

                return Signature[0].ToByteArray();
            }
        }
        
        public byte[] S
        {
            get
            {
                if (Signature == null || Signature.Length != 2)
                    return null;

                return Signature[1].ToByteArray();
            }
        }
    }
}