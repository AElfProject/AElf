using System;
using Ed25519;

namespace AElf.Sdk.CSharp;

public static class BuiltIns
{
    public static bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
    {
        try
        {
            var instance = new Rebex.Security.Cryptography.Ed25519();
            instance.FromPublicKey(publicKey);
            return instance.VerifyMessage(message, signature);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}