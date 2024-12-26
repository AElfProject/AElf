using System;
using Rebex.Security.Cryptography;

namespace AElf.Cryptography.EdDSA
{
    public static class EdDsaHelper
    {
        public static bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
        {
            try
            {
                var instance = new Ed25519();
                instance.FromPublicKey(publicKey);
                return instance.VerifyMessage(message, signature);
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}