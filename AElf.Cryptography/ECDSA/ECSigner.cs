using System;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECSigner
    {
        public ECSignature Sign(ECKeyPair keyPair, byte[] data)
        {
            var recSig = new byte[65];
            var compactSig = new byte[65];
            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.SignRecoverable(recSig, data, keyPair.PrivateKey);
                secp256k1.RecoverableSignatureSerializeCompact(compactSig, out var recoverID, recSig);
                compactSig[64] = (byte)recoverID;
            }
            
            return new ECSignature(compactSig);
        }
    }
}