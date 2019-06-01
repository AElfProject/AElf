using System;
using System.Linq;
using Acs4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// In this method, `Context.CurrentBlockTime` is the time one miner start request his next consensus command.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            Assert(input.Value.Any(), "Invalid public key.");

            if (Context.CurrentHeight == 1) return GetInvalidConsensusCommand();

            var publicKey = input.Value.ToHex();

            if (!TryToGetCurrentRoundInformation(out var currentRound))
            {
                Assert(false, $"Failed to get current round information in height {Context.CurrentHeight}");
            }

            var behaviour = GetConsensusBehaviour(currentRound, publicKey);

            Context.LogDebug(() => currentRound.GetLogs(publicKey, behaviour));

            return behaviour == AElfConsensusBehaviour.Nothing
                ? GetInvalidConsensusCommand() // Handle this situation previously.
                : GetConsensusCommand(behaviour, currentRound, publicKey);
        }

        public override BytesValue GetInformationToUpdateConsensus(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);

            Assert(triggerInformation.PublicKey.Any(), "Invalid public key.");

            if (!TryToGetCurrentRoundInformation(out var currentRound))
            {
                Assert(false, "Failed to get current round information.");
            }

            var publicKeyBytes = triggerInformation.PublicKey;
            var publicKey = publicKeyBytes.ToHex();

            LogIfPreviousMinerHasNotProduceEnoughTinyBlocks(currentRound, publicKey);

            switch (triggerInformation.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return GetInformationToUpdateConsensusToPublishOutValue(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.TinyBlock:
                    return GetInformationToUpdateConsensusForTinyBlock(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.NextRound:
                    return GetInformationToUpdateConsensusForNextRound(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.NextTerm:
                    return GetInformationToUpdateConsensusForNextTerm(publicKey, triggerInformation)
                        .ToBytesValue();
                default:
                    return new BytesValue();
            }
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);
            // Some basic checks.
            Assert(triggerInformation.PublicKey.Any(),
                "Data to request consensus information should contain public key.");

            var publicKey = triggerInformation.PublicKey;
            var consensusInformation = new AElfConsensusHeaderInformation();
            consensusInformation.MergeFrom(GetInformationToUpdateConsensus(input).Value);
            var round = consensusInformation.Round;
            var behaviour = consensusInformation.Behaviour;
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(publicKey.ToHex()))
                        }
                    };
                case AElfConsensusBehaviour.TinyBlock:
                    var minerInRound = round.RealTimeMinersInformation[publicKey.ToHex()];
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateTinyBlockInformation),
                                new TinyBlockInput
                                {
                                    ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
                                    ProducedBlocks = minerInRound.ProducedBlocks,
                                    RoundId = round.RoundId
                                })
                        }
                    };
                case AElfConsensusBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound), round)
                        }
                    };
                case AElfConsensusBehaviour.NextTerm:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextTerm), round)
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            var extraData = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value.ToByteArray());
            return ValidateBeforeExecution(extraData);
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input1)
        {
            var input = new AElfConsensusHeaderInformation();
            input.MergeFrom(input1.Value);
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var isContainPreviousInValue =
                    input.Behaviour != AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                if (input.Round.GetHash(isContainPreviousInValue) != currentRound.GetHash(isContainPreviousInValue))
                {
                    Context.LogDebug(() => $"Round information of block header:\n{input.Round}");
                    Context.LogDebug(() => $"Round information of executing result:\n{currentRound}");
                    return new ValidationResult
                    {
                        Success = false, Message = "Current round information is different with consensus extra data."
                    };
                }
            }

            return new ValidationResult {Success = true};
        }
    }
}