using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECVerifier
    {
        public bool Verify(ECSignature signature, byte[] data)
        {
            if (signature == null || data == null)
                return false;

            using (var secp256k1 = new Secp256k1())
            {
                // recover
                byte[] publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
                secp256k1.Recover(publicKeyOutput, signature.SigBytes, data);
                
                return secp256k1.Verify(signature.SigBytes, data, publicKeyOutput);
            }
        }
    }
}