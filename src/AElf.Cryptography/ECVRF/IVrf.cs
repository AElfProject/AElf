using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;

namespace AElf.Cryptography.ECVRF;

public struct Proof
{
    public byte[] Beta { get; set; }
    public byte[] Pi { get; set; }
}

public struct ProofInput
{
    public Point Gamma { get; set; }
    public BigInteger C { get; set; }
    public BigInteger S { get; set; }
}

public struct VrfConfig
{
    public byte SuiteString { get; private set; }

    public X9ECParameters EcParameters { get; private set; }

    public VrfConfig(byte suiteString, X9ECParameters ecParameters)
    {
        SuiteString = suiteString;
        EcParameters = ecParameters;
    }
}

public interface IVrf
{
    Proof Prove(ECKeyPair keyPair, byte[] alpha);
    byte[] Verify(byte[] publicKey, byte[] alpha, byte[] pi);
}