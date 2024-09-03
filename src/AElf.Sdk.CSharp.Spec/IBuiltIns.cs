namespace AElf.Sdk.CSharp.Spec;

public interface IBuiltIns
{
    bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey);
}