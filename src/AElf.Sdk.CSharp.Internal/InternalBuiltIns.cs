using System;
using AElf.Sdk.CSharp.Spec;

namespace AElf.Sdk.CSharp.Internal;

internal class InternalBuiltIns : IBuiltIns
{
    public static void Initialize()
    {
        // call this method to ensure this assembly is loaded in the runtime.  
    }
    public bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
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