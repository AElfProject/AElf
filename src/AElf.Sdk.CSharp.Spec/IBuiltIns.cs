namespace AElf.Sdk.CSharp.Spec;

public interface IBuiltIns
{
    bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey);
    byte[] Keccak256(byte[] message);
    (byte[] x, byte[] y) Bn254G1Mul(byte[] x1, byte[] y1, byte[] s);
    (byte[] x3, byte[] y3) Bn254G1Add(byte[] x1, byte[] y1, byte[] x2, byte[] y2);
    bool Bn254Pairing((byte[], byte[], byte[], byte[], byte[], byte[])[] input);
}