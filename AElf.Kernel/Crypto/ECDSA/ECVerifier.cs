using Org.BouncyCastle.Crypto.Signers;

namespace AElf.Kernel.Crypto.ECDSA
{
    public class ECVerifier
    {
        private ECKeyPair _keyPair { get; set; }
        
        public ECVerifier(ECKeyPair keyPair)
        {
            _keyPair = keyPair;
        }
        
        public bool Verify(ECSignature signature, byte[] data)
        {
            if (signature == null || _keyPair == null || data == null)
                return false;
            
            ECDsaSigner verifier = new ECDsaSigner();
            verifier.Init(false, _keyPair.PublicKey);

            return verifier.VerifySignature(data, signature.Signature[0], signature.Signature[1]);
        }
    }
}