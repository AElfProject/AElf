using Bn254.Net;

namespace ZkWasmVerifier;

public interface IAggregatorVerifierCoreStep
{
    UInt256[] VerifyProof(UInt256[] transcript, UInt256[] aux, UInt256[] buf);
}