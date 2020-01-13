using Acs1;
using Acs6;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.CommitmentScheme
{
    public class CommitmentSchemeContract : CommitmentSchemeContractContainer.CommitmentSchemeContractBase
    {
        public override RandomNumberOrder RequestRandomNumber(Hash input)
        {
            // The input will be treated as a commitment of the sender.
            State.Commitments[Context.Sender] = input;
            var usingPreviousInValueInformation = GetPreviousInValueInformation();
            State.PreviousInValueInformations[Context.Sender] = usingPreviousInValueInformation;
            return new RandomNumberOrder
            {
                TokenHash = input,
                BlockHeight = Context.CurrentHeight
            };
        }

        public override Hash GetRandomNumber(Hash input)
        {
            return base.GetRandomNumber(input);
        }

        /// <summary>
        /// Get Latest Out Value from AEDPoS Contract.
        /// </summary>
        /// <returns></returns>
        private PreviousInValueInformation GetPreviousInValueInformation()
        {
            throw new System.NotImplementedException();
        }
    }
}