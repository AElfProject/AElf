using System.Security.Cryptography;
using AElf.Cryptography.Core;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;

namespace AElf.Cryptography.ECVRF
{

    public struct ProofInput
    {
        public IECPoint Gamma { get; set; }
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

    public interface IHasherFactory
    {
        HashAlgorithm Create();
    }

    public interface IVrf
    {
        byte[] Prove(ECKeyPair keyPair, byte[] alpha);
        byte[] Verify(byte[] publicKey, byte[] alpha, byte[] pi);
    }
}