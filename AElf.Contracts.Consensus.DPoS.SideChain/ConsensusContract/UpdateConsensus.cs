using System;
using System.Linq;
using System.Text;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        private long CurrentAge => State.AgeField.Value;
        
        public override DPoSHeaderInformation GetInformationToUpdateConsensus(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var publicKey = input.PublicKey;
            var currentBlockTime = Context.CurrentBlockTime;
            var behaviour = input.Behaviour;

            Assert(TryToGetCurrentRoundInformation(out var currentRound),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed,
                    "Failed to get current round information."));

            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    Assert(input.RandomHash != null, "Random hash should not be null.");

                    var inValue = currentRound.CalculateInValue(input.RandomHash);
                    var outValue = Hash.FromMessage(inValue);
                    var signature = Hash.FromTwoHashes(outValue, input.RandomHash); // Just initial signature value.
                    var previousInValue = Hash.Empty; // Just initial previous in value.

                    if (TryToGetPreviousRoundInformation(out var previousRound))
                    {
                        signature = previousRound.CalculateSignature(inValue);
                        LogVerbose($"Previous random hash: {input.PreviousRandomHash.ToHex()}");
                        if (input.PreviousRandomHash != Hash.Empty)
                        {
                            // If PreviousRandomHash is Hash.Empty, it means the sender unable or unwilling to publish his previous in value.
                            previousInValue = previousRound.CalculateInValue(input.PreviousRandomHash);
                        }
                    }

                    var updatedRound = currentRound.ApplyNormalConsensusData(publicKey.ToHex(), previousInValue,
                        outValue, signature, currentBlockTime);

                    ShareAndRecoverInValue(updatedRound, previousRound, inValue, publicKey.ToHex());

                    // To publish Out Value.
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour,
                    };
                case DPoSBehaviour.NextRound:
                    Assert(
                        GenerateNextRoundInformation(currentRound, currentBlockTime, out var nextRound),
                        "Failed to generate next round information.");
                    nextRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks += 1;
                    Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
                    nextRound.ExtraBlockProducerOfPreviousRound = publicKey.ToHex();
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    };
                default:
                    return new DPoSHeaderInformation();
            }
        }

        private void ShareAndRecoverInValue(Round round, Round previousRound, Hash inValue, string publicKey)
        {
            var minersCount = round.RealTimeMinersInformation.Count;
            var minimumCount = (int) (minersCount * 2d / 3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var secretShares = SecretSharingHelper.EncodeSecret(inValue.ToHex(), minimumCount, minersCount);
            foreach (var pair in round.RealTimeMinersInformation.OrderBy(m => m.Value.Order))
            {
                var currentPublicKey = pair.Key;

                if (currentPublicKey == publicKey)
                {
                    continue;
                }

                // Encrypt every secret share with other miner's public key, then fill own EncryptedInValues field.
                var plainMessage = Encoding.UTF8.GetBytes(secretShares[pair.Value.Order - 1]);
                var receiverPublicKey = ByteArrayHelpers.FromHexString(currentPublicKey);
                var encryptedInValue = Context.EncryptMessage(receiverPublicKey, plainMessage);
                round.RealTimeMinersInformation[publicKey].EncryptedInValues
                    .Add(currentPublicKey, ByteString.CopyFrom(encryptedInValue));

                if (previousRound.RoundId == 0 || round.TermNumber != previousRound.TermNumber)
                {
                    continue;
                }

                var encryptedInValues = previousRound.RealTimeMinersInformation[currentPublicKey].EncryptedInValues;
                if (encryptedInValues.Any())
                {
                    var interestingMessage = encryptedInValues[publicKey];
                    var senderPublicKey = ByteArrayHelpers.FromHexString(currentPublicKey);
                    // Decrypt every miner's secret share then add a result to other miner's DecryptedInValues field.
                    var decryptedInValue = Context.DecryptMessage(senderPublicKey, interestingMessage.ToByteArray());
                    round.RealTimeMinersInformation[pair.Key].DecryptedPreviousInValues
                        .Add(publicKey, ByteString.CopyFrom(decryptedInValue));
                }

                if (pair.Value.DecryptedPreviousInValues.Count < minimumCount)
                {
                    continue;
                }

                Context.LogDebug(() => "Now it's enough to recover previous in values.");

                // Try to recover others' previous in value.
                var orders = pair.Value.DecryptedPreviousInValues.Select((t, i) =>
                        previousRound.RealTimeMinersInformation.Values
                            .First(m => m.PublicKey == pair.Value.DecryptedPreviousInValues.Keys.ToList()[i]).Order)
                    .ToList();

                var previousInValue = Hash.LoadHex(SecretSharingHelper.DecodeSecret(
                    pair.Value.DecryptedPreviousInValues.Values.ToList()
                        .Select(s => Encoding.UTF8.GetString(s.ToByteArray())).ToList(),
                    orders, minimumCount));
                if (round.RealTimeMinersInformation[pair.Key].PreviousInValue != null &&
                    round.RealTimeMinersInformation[pair.Key].PreviousInValue != previousInValue)
                {
                    Context.LogDebug(() => "Different previous in value.");
                }
                round.RealTimeMinersInformation[pair.Key].PreviousInValue = previousInValue;
            }
        }
        
        private bool GenerateNextRoundInformation(Round currentRound, DateTime blockTime, out Round nextRound)
        {
            TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp);
            var result = currentRound.GenerateNextRoundInformation(blockTime, blockchainStartTimestamp, out nextRound);
            nextRound.BlockchainAge = CurrentAge;
            return result;
        }

        public override TransactionList GenerateConsensusTransactions(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");

            var publicKey = input.PublicKey;
            var consensusInformation = GetInformationToUpdateConsensus(input);
            var round = consensusInformation.Round;
            var behaviour = consensusInformation.Behaviour;
            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(publicKey.ToHex()))
                        }
                    };
                case DPoSBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound), round)
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}