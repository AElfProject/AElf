using System;
using System.Linq;
using AElf.Sdk.CSharp;
using Bn254.Net;
using Google.Protobuf.WellKnownTypes;
using ZkWasmVerifier;

namespace AElf.Contracts.ZkWasmVerifier
{
    public class ZkWasmVerifier : ZkWasmVerifierContainer.ZkWasmVerifierBase
    {
        public override Empty Verify(VerifyInput input)
        {
            var steps = new Object[]
            {
                new AggregatorVerifierCoreStep1(),
                new AggregatorVerifierCoreStep2(),
                new AggregatorVerifierCoreStep3(),
            };

            var proof = input.Proof.Select(x => x.ToUInt256()).ToArray();
            var aux = input.Aux.Select(x => x.ToUInt256()).ToArray().ToArray();
            var verifyInstance = input.VerifyInstance.Select(x => x.ToUInt256()).ToArray();
            var targetInstance = input.TargetInstance.Select(x => x.Value.Select(y => y.ToUInt256()).ToArray())
                .ToArray();
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
                foreach (var step in steps)
                {
                    buf = ((IAggregatorVerifierCoreStep)step).VerifyProof(proof, aux, buf);
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

            return new Empty();
        }
    }
}