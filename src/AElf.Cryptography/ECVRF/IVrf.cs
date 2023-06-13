using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography.ECVRF;

public struct Proof
{
    public byte[] Beta { get; set; }
    public byte[] Pi { get; set; }
}

public interface IVrf
{
    Proof Prove(ECKeyPair keyPair, byte[] alpha);
    byte[] Verify(byte[] publicKey, Proof proof);
}