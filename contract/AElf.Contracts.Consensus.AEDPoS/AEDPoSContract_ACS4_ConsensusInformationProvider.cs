using System;
using System.Linq;
using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// In this method, `Context.CurrentBlockTime` is the time one miner start request his next consensus command.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            _processingBlockMinerPubkey = input.Value.ToHex();

            if (Context.CurrentHeight < 2) return ConsensusCommandProvider.InvalidConsensusCommand;

            if (!TryToGetCurrentRoundInformation(out var currentRound))
                return ConsensusCommandProvider.InvalidConsensusCommand;

            if (!currentRound.IsInMinerList(_processingBlockMinerPubkey))
                return ConsensusCommandProvider.InvalidConsensusCommand;

            var blockchainStartTimestamp = GetBlockchainStartTimestamp();

            var behaviour = IsMainChain
                ? new MainChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey, GetMaximumBlocksCount(),
                        Context.CurrentBlockTime, blockchainStartTimestamp, State.TimeEachTerm.Value)
                    .GetConsensusBehaviour()
                : new SideChainConsensusBehaviourProvider(currentRound, _processingBlockMinerPubkey, GetMaximumBlocksCount(),
                    Context.CurrentBlockTime).GetConsensusBehaviour();

            Context.LogDebug(() => $"{currentRound.ToString(_processingBlockMinerPubkey)}\nArranged behaviour: {behaviour.ToString()}");

            return behaviour == AElfConsensusBehaviour.Nothing
                ? ConsensusCommandProvider.InvalidConsensusCommand
                : GetConsensusCommand(behaviour, currentRound, _processingBlockMinerPubkey);
        }

        public override BytesValue GetConsensusExtraData(BytesValue input)
        {
            return GetConsensusBlockExtraData(input);
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);
            // Some basic checks.
            Assert(triggerInformation.Pubkey.Any(),
                "Data to request consensus information should contain public key.");

            var pubkey = triggerInformation.Pubkey;
            var consensusInformation = new AElfConsensusHeaderInformation();
            consensusInformation.MergeFrom(GetConsensusBlockExtraData(input, true).Value);
            var round = consensusInformation.Round;
            var behaviour = consensusInformation.Behaviour;
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(pubkey.ToHex()))
                        }
                    };
                case AElfConsensusBehaviour.TinyBlock:
                    var minerInRound = round.RealTimeMinersInformation[pubkey.ToHex()];
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
                var isContainPreviousInValue = !currentRound.IsMinerListJustChanged;
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