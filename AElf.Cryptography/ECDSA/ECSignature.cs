using Org.BouncyCastle.Math;

namespace AElf.Cryptography.ECDSA
{
    public class ECSignature
    {
        public BigInteger[] Signature { get; }

        internal ECSignature(BigInteger[] signature)
        {
            Signature = signature;
        }

        public ECSignature(byte[] r, byte[] s)
        {
            var rs = new BigInteger[2];
            rs[0] = new BigInteger(1, r);
            rs[1] = new BigInteger(1, s);
            Signature = rs;
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