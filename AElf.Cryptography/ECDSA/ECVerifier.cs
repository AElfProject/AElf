using System;
using System.Linq;
using AElf.Common;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECVerifier
    {
        // TODO: maybe need refactor, both Cryptography and CryptoHelpers expose public method. 
        public bool Verify(ECSignature signature, byte[] data)
        {
            if (signature == null || data == null)
                return false;

            try
            {
                return CryptoHelpers.Verify(signature.SigBytes, data);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}