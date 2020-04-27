using System.Linq;
using Acs6;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.CommitmentScheme
{
    // ReSharper disable InconsistentNaming
    public class CommitmentSchemeContract : CommitmentSchemeContractContainer.CommitmentSchemeContractBase
    {
        public override RandomNumberOrder RequestRandomNumber(Hash input)
        {
            // The input will be treated as a commitment of the sender.
            State.Commitments[Context.Sender] = input;
            var requestSlot = GetCurrentSlotInformation();
            State.RequestSlots[Context.Sender] = requestSlot;
            return new RandomNumberOrder
            {
                TokenHash = input,
                BlockHeight = requestSlot.BlockHeight.Add(CommitmentSchemeContractConstants.WaitBlockHeight)
            };
        }

        public override Hash GetRandomNumber(Hash input)
        {
            var requestSlot = State.RequestSlots[Context.Sender];
            Assert(
                requestSlot != null && requestSlot.BlockHeight.Add(CommitmentSchemeContractConstants.WaitBlockHeight) <=
                Context.CurrentHeight, "Incorrect request slot.");
            var userCommitment = State.Commitments[Context.Sender];
            Assert(HashHelper.ComputeFrom(input) == userCommitment, "Incorrect commitment.");
            var properInValue = GetNextInValueOfSlot(State.RequestSlots[Context.Sender]);
            return HashHelper.ConcatAndCompute(input, properInValue);
        }

        private Hash GetNextInValueOfSlot(RequestSlot requestSlot)
        {
            RequireAEDPoSContractStateSet();
            var round = State.AEDPoSContract.GetRoundInformation.Call(new Int64Value
            {
                Value = requestSlot.RoundNumber
            });
            if (requestSlot.Order < round.RealTimeMinersInformation.Count)
            {
                return round.RealTimeMinersInformation.Values
                    .FirstOrDefault(i => i.Order > requestSlot.Order && i.PreviousInValue != null)
                    ?.PreviousInValue;
            }

            var nextRound = State.AEDPoSContract.GetRoundInformation.Call(new Int64Value
            {
                Value = requestSlot.RoundNumber.Add(1)
            });
            return nextRound.RealTimeMinersInformation.Values
                .FirstOrDefault(i => i.PreviousInValue != null)
                ?.PreviousInValue;
        }

        /// <summary>
        /// Get Latest Out Value from AEDPoS Contract.
        /// </summary>
        /// <returns></returns>
        private RequestSlot GetCurrentSlotInformation()
        {
            RequireAEDPoSContractStateSet();
            var round = State.AEDPoSContract.GetCurrentRoundInformation.Call(new Empty());
            var lastMinedMiner = round.RealTimeMinersInformation.Values.Where(i => i.OutValue != null)
                .OrderByDescending(i => i.Order).FirstOrDefault();

            return new RequestSlot
            {
                Order = lastMinedMiner?.Order ?? 0,
                RoundNumber = round.RoundNumber,
                BlockHeight = Context.CurrentHeight
            };
        }

        private void RequireAEDPoSContractStateSet()
        {
            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }
        }
    }
}