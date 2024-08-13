using System;
using System.Linq;
using AElf.Sdk.CSharp;
using Bn254.Net;

namespace ZkWasmVerifier;

public static class ZkWasmVerifier
{
    private static IAggregatorVerifierCoreStep[] _steps = new IAggregatorVerifierCoreStep[]
    {
        new AggregatorVerifierCoreStep1(),
        new AggregatorVerifierCoreStep2(),
        new AggregatorVerifierCoreStep3(),
    };

    public static void Verify(UInt256[] proof, UInt256[] verifyInstance, UInt256[] aux, UInt256[][] targetInstance)
    {
        var buf = new UInt256[43];

        var forCalulatingKeccakHash = targetInstance.SelectMany(x => x)
            .Concat(verifyInstance).ToArray();
        buf[2] = AggregatorLib.HashInstances(forCalulatingKeccakHash);

        UInt256[] verifyCircuitPairingBuf = new UInt256[12];
        {
            // step 1: calculate verify circuit instance commitment
            AggregatorConfig.CalcVerifyCircuitLagrange(buf);

            // step 2: calculate challenge
            // take transcript[0..102]
            // calculate challenges and store them in buf[0..10]
            AggregatorConfig.GetChallenges(proof, buf);

            // step 3: calculate verify circuit pair
            foreach (var step in _steps)
            {
                buf = step.VerifyProof(proof, aux, buf);
            }

            verifyCircuitPairingBuf[0] = buf[0];
            verifyCircuitPairingBuf[1] = buf[1];
            verifyCircuitPairingBuf[6] = buf[2];
            verifyCircuitPairingBuf[7] = buf[3];
            if (verifyCircuitPairingBuf[0].IsZero() || verifyCircuitPairingBuf[1].IsZero())
            {
                throw new AssertionException("invalid w point");
            }

            if (verifyCircuitPairingBuf[6].IsZero() || verifyCircuitPairingBuf[7].IsZero())
            {
                throw new AssertionException("invalid g point");
            }
        }
        var checked_ = false;
        AggregatorConfig.FillVerifyCircuitsG2(verifyCircuitPairingBuf);
        checked_ = AggregatorLib.Pairing(verifyCircuitPairingBuf);
        if (!checked_)
        {
            throw new AssertionException("pairing check failed");
        }
    }
}