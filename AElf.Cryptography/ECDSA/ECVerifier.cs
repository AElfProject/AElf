using System;
using System.Linq;
using AElf.Common;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECVerifier
    {
        public bool Verify(ECSignature signature, byte[] data)
        {
            if (signature == null || data == null)
                return false;

            try
            {
                using (var secp256k1 = new Secp256k1())
                {
                    // recover
                    byte[] publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
                    var recSig = new byte[65];
                    var compactSig = signature.SigBytes;
                    secp256k1.RecoverableSignatureParseCompact(recSig, compactSig, compactSig.Last());
                    secp256k1.Recover(publicKeyOutput, recSig, data);
                    return secp256k1.Verify(recSig, data, publicKeyOutput);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}